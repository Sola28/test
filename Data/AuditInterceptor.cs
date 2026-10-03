using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;

    // Entities we don't audit to avoid noise / recursion.
    private static readonly HashSet<string> Skipped =
    [
        nameof(AuditEntry),
        "IdentityUserClaim",
        "IdentityUserRole",
        "IdentityUserToken",
        "IdentityUserLogin",
        "IdentityRoleClaim"
    ];

    // Properties that produce noise but carry no meaning.
    private static readonly HashSet<string> IgnoredProps =
    [
        "ConcurrencyStamp",
        "SecurityStamp",
        "PasswordHash",
        "RowVersion"
    ];

    public AuditInterceptor(IHttpContextAccessor http) => _http = http;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteAudits(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        WriteAudits(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAudits(DbContext? ctx)
    {
        if (ctx is null) return;

        var user = _http.HttpContext?.User?.Identity?.Name ?? "system";
        var now = DateTime.Now;

        // Snapshot the entries first — we mutate the change tracker by adding AuditEntries.
        var entries = ctx.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added
                                 or EntityState.Modified
                                 or EntityState.Deleted)
            .Where(e => !Skipped.Contains(e.Metadata.ClrType.Name))
            .ToList();

        var audits = new List<AuditEntry>();

        foreach (var e in entries)
        {
            var action = e.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Unknown"
            };

            // Skip "Updated" rows where nothing meaningful actually changed.
            if (e.State == EntityState.Modified)
            {
                var changed = e.Properties.Any(p =>
                    p.IsModified &&
                    !IgnoredProps.Contains(p.Metadata.Name) &&
                    !Equals(p.OriginalValue, p.CurrentValue));

                if (!changed) continue;
            }

            var details = BuildDetails(e);
            var key = TryKey(e);

            audits.Add(new AuditEntry
            {
                At = now,
                User = user,
                Action = action,
                Entity = e.Metadata.ClrType.Name,
                EntityKey = key,
                Details = details
            });
        }

        if (audits.Count > 0)
            ctx.Set<AuditEntry>().AddRange(audits);
    }

    private static string TryKey(EntityEntry e)
    {
        // Prefer a business key when we recognise one.
        string? Read(string prop) =>
            e.Properties.FirstOrDefault(p => p.Metadata.Name == prop)?.CurrentValue?.ToString()
            ?? e.Properties.FirstOrDefault(p => p.Metadata.Name == prop)?.OriginalValue?.ToString();

        foreach (var candidate in new[] { "Accession", "Code", "MedicalRecordNumber", "UserName", "Email", "Name" })
        {
            var v = Read(candidate);
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        // Fall back to the primary key.
        var pk = e.Metadata.FindPrimaryKey();
        if (pk is not null)
        {
            var parts = pk.Properties
                .Select(p => e.Property(p.Name).CurrentValue
                          ?? e.Property(p.Name).OriginalValue)
                .Where(v => v is not null)
                .Select(v => v!.ToString());
            var joined = string.Join(",", parts);
            if (!string.IsNullOrWhiteSpace(joined)) return joined;
        }

        return "?";
    }

    private static string? BuildDetails(EntityEntry e)
    {
        if (e.State == EntityState.Added)
        {
            var created = e.Properties
                .Where(p => !IgnoredProps.Contains(p.Metadata.Name))
                .Where(p => p.CurrentValue is not null)
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            return created.Count == 0 ? null : JsonSerializer.Serialize(created);
        }

        if (e.State == EntityState.Deleted)
        {
            var deleted = e.Properties
                .Where(p => !IgnoredProps.Contains(p.Metadata.Name))
                .Where(p => p.OriginalValue is not null)
                .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            return deleted.Count == 0 ? null : JsonSerializer.Serialize(deleted);
        }

        // Modified: capture before/after for changed props only.
        var diff = new Dictionary<string, object?>();
        foreach (var p in e.Properties)
        {
            if (IgnoredProps.Contains(p.Metadata.Name)) continue;
            if (!p.IsModified) continue;
            if (Equals(p.OriginalValue, p.CurrentValue)) continue;

            diff[p.Metadata.Name] = new { from = p.OriginalValue, to = p.CurrentValue };
        }
        return diff.Count == 0 ? null : JsonSerializer.Serialize(diff);
    }
}