using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services.Instruments.Parsing;

namespace ZDLISPlus.Web.Services.Instruments;

/// <summary>
/// Takes a parsed outcome, matches it to a LabOrder via accession,
/// resolves each instrument code through the InstrumentTestMap,
/// and writes OrderTestResult rows so the Results module sees the values.
/// </summary>
public class ResultDispatcher
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ResultDispatcher> _log;

    public ResultDispatcher(IServiceScopeFactory scopes, ILogger<ResultDispatcher> log)
    {
        _scopes = scopes;
        _log = log;
    }

    public async Task<int> DispatchAsync(int instrumentId, ParseOutcome outcome)
    {
        if (!outcome.Success || outcome.Results.Count == 0)
            return 0;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();

        var instrument = await db.Instruments
            .Include(i => i.TestMaps)
            .FirstOrDefaultAsync(i => i.Id == instrumentId);

        if (instrument is null)
        {
            _log.LogWarning("Dispatch: instrument {Id} not found", instrumentId);
            return 0;
        }

        var sampleId = outcome.SampleId?.Trim();
        if (string.IsNullOrWhiteSpace(sampleId))
        {
            _log.LogWarning("Instrument {Id}: no sample id in message", instrumentId);
            return 0;
        }

        // Find the order by accession
        var order = await db.LabOrders
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest).ThenInclude(c => c.Children)
            .FirstOrDefaultAsync(o => o.Accession == sampleId);

        if (order is null)
        {
            _log.LogWarning("Instrument {Id}: no order for accession {Acc}", instrumentId, sampleId);
            return 0;
        }

        // Build component lookup: componentTestId -> (orderedTestId, component)
        var componentLookup = new Dictionary<int, (int OrderedTestId, TestCatalog Component)>();
        foreach (var ot in order.Tests)
        {
            var components = ot.CatalogTest.IsProfile && ot.CatalogTest.Children.Any()
                ? ot.CatalogTest.Children
                : new List<TestCatalog> { ot.CatalogTest };

            foreach (var c in components)
                componentLookup[c.Id] = (ot.CatalogTestId, c);
        }

        var saved = 0;

        foreach (var r in outcome.Results)
        {
            // Exact match on instrument code
            var map = instrument.TestMaps.FirstOrDefault(m =>
                m.IsActive &&
                string.Equals(m.InstrumentCode, r.InstrumentCode, StringComparison.OrdinalIgnoreCase));

            if (map is null)
            {
                _log.LogWarning("Instrument {Id}: no map for code {Code}", instrumentId, r.InstrumentCode);
                continue;
            }

            if (!componentLookup.TryGetValue(map.ComponentTestId, out var entry))
            {
                _log.LogWarning("Instrument {Id}: order {Acc} does not contain component {Comp}",
                    instrumentId, sampleId, map.ComponentTestId);
                continue;
            }

            var value = r.Value;
            if (map.Factor.HasValue && decimal.TryParse(value, out var parsed))
                value = (parsed * map.Factor.Value).ToString("0.####");

            var row = await db.OrderTestResults.FirstOrDefaultAsync(x =>
                x.OrderId == order.Id &&
                x.OrderedTestId == entry.OrderedTestId &&
                x.ComponentTestId == entry.Component.Id);

            if (row is null)
            {
                row = new OrderTestResult
                {
                    OrderId = order.Id,
                    OrderedTestId = entry.OrderedTestId,
                    ComponentTestId = entry.Component.Id
                };
                db.OrderTestResults.Add(row);
            }

            row.Value = value;
            row.Flag = r.Flag ?? ResultFlagService.ComputeFlag(value, entry.Component.ReferenceRange);
            row.EnteredBy = $"instrument:{instrument.Name}";
            row.EnteredAt = DateTime.Now;
            saved++;
        }

        if (saved > 0 && order.Status == OrderStatus.Collected)
            order.Status = OrderStatus.Processing;

        if (saved > 0)
        {
            db.AuditTrail.Add(new AuditEntry
            {
                User = $"instrument:{instrument.Name}",
                Action = "InstrumentResult",
                Entity = "LabOrder",
                EntityKey = order.Accession,
                Details = $"Imported {saved} results from {instrument.Name}"
            });
        }

        await db.SaveChangesAsync();
        _log.LogInformation("Instrument {Id}: saved {Count} results for {Acc}",
            instrumentId, saved, sampleId);
        return saved;
    }
}