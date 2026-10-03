using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services;

public class AuditService
{
    private readonly LabDbContext _db;

    public AuditService(LabDbContext db) => _db = db;

    /// <summary>Write an audit entry. Call after any state-changing action.</summary>
    public async Task LogAsync(string user, string action, string entity, string entityKey, string? details = null)
    {
        _db.AuditTrail.Add(new AuditEntry
        {
            User = string.IsNullOrWhiteSpace(user) ? "anonymous" : user,
            Action = action,
            Entity = entity,
            EntityKey = entityKey,
            Details = details,
            At = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>Convenience overload for a ClaimsPrincipal (e.g. HttpContext.User).</summary>
    public Task LogAsync(ClaimsPrincipal? principal, string action, string entity, string entityKey, string? details = null)
        => LogAsync(principal?.Identity?.Name ?? "anonymous", action, entity, entityKey, details);

    public Task<List<AuditEntry>> RecentAsync(int take = 50)
        => _db.AuditTrail
            .OrderByDescending(a => a.At)
            .Take(take)
            .ToListAsync();

    public Task<List<AuditEntry>> ForEntityAsync(string entity, string entityKey)
        => _db.AuditTrail
            .Where(a => a.Entity == entity && a.EntityKey == entityKey)
            .OrderByDescending(a => a.At)
            .ToListAsync();
}