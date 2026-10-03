using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;

namespace ZDLISPlus.Web.Services;

public class LabDataService
{
    private readonly LabDbContext _db;
    private readonly InstrumentService _instruments;
    public LabDataService(LabDbContext db, InstrumentService instruments) => (_db, _instruments) = (db, instruments);

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var stats = new List<StatCard>
        {
            new() { Label = "Patients today",  Value = (await _db.LabOrders.Where(o => o.OrderedAt >= today).Select(o => o.PatientId).Distinct().CountAsync()).ToString(), Sub = "↑ 8.2% vs. yesterday", Accent = "teal",  IconSvg = "users" },
            new() { Label = "Active orders",   Value = (await _db.LabOrders.CountAsync(o => o.Status != OrderStatus.Released)).ToString(), Sub = "Awaiting processing", Accent = "blue", IconSvg = "clipboard" },
            new() { Label = "For validation",  Value = (await _db.LabOrders.CountAsync(o => o.Status == OrderStatus.ForValidation)).ToString(), Sub = "Oldest pending review", Accent = "amber", IconSvg = "shield" },
            new() { Label = "Critical results",Value = (await _db.LabOrders.CountAsync(o => o.Status == OrderStatus.Critical)).ToString(), Sub = "Immediate review required", Accent = "red", IconSvg = "alert" }
        };

        var workload = new List<WorkloadBar>
        {
            new() { Hour = "7:00",  Value = 24 }, new() { Hour = "8:00", Value = 39 },
            new() { Hour = "9:00",  Value = 57 }, new() { Hour = "10:00", Value = 51 },
            new() { Hour = "11:00", Value = 78 }, new() { Hour = "12:00", Value = 63 },
            new() { Hour = "13:00", Value = 87, IsPeak = true }, new() { Hour = "14:00", Value = 69 },
            new() { Hour = "15:00", Value = 54 }
        };

        var orders = await _db.LabOrders
            .Include(o => o.Patient).Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .OrderByDescending(o => o.OrderedAt)
            .Take(8).ToListAsync();

        return new DashboardViewModel
        {
            TodayLabel = DateTime.Now.ToString("dddd, MMMM d").ToUpperInvariant(),
            Stats = stats,
            Workload = workload,
            Instruments = await _instruments.AllAsync(),
            RecentOrders = orders,
            CompletedToday = await _db.LabOrders.CountAsync(o => o.OrderedAt >= today && o.Status == OrderStatus.Released),
            InProgressToday = await _db.LabOrders.CountAsync(o => o.OrderedAt >= today && o.Status == OrderStatus.Processing),
            AverageTatMinutes = 42
        };
    }

    public async Task<ReportsViewModel> GetReportsAsync(int days)
    {
        var from = DateTime.Today.AddDays(-days);
        var orders = await _db.LabOrders
            .Include(o => o.Patient).Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .Where(o => o.OrderedAt >= from)
            .ToListAsync();

        var daily = Enumerable.Range(0, days).Select(offset =>
        {
            var d = DateTime.Today.AddDays(-(days - 1 - offset));
            return new WorkloadBar { Hour = d.ToString("ddd"), Value = orders.Count(o => o.OrderedAt.Date == d) };
        }).ToList();

        var byDiscipline = orders
            .SelectMany(o => o.Tests)
            .Where(t => t.CatalogTest is not null)
            .GroupBy(t => t.CatalogTest.Discipline)
            .Select(g => (Discipline: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var released = orders.Where(o => o.Status == OrderStatus.Released || o.Status == OrderStatus.Critical)
            .OrderByDescending(o => o.OrderedAt).Take(50).ToList();

        return new ReportsViewModel
        {
            Days = days,
            DailyVolume = daily,
            ByDiscipline = byDiscipline,
            Released = released,
            TotalOrders = orders.Count,
            TotalRevenue = orders.SelectMany(o => o.Tests).Sum(t => t.CatalogTest?.Price ?? 0m),
            AverageTat = 42
        };
    }

    public async Task<AuditViewModel> GetAuditAsync(
    string? q,
    string? action,
    string? entity,
    DateTime? from,
    DateTime? to,
    int page,
    int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 10) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var query = _db.AuditTrail.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a =>
                a.User.Contains(q) ||
                a.Entity.Contains(q) ||
                a.EntityKey.Contains(q) ||
                (a.Details != null && a.Details.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);

        if (from.HasValue)
        {
            var start = from.Value.Date;
            query = query.Where(a => a.At >= start);
        }

        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            query = query.Where(a => a.At < end);
        }

        var total = await query.CountAsync();

        var entries = await query
            .OrderByDescending(a => a.At)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var actions = await _db.AuditTrail
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var entities = await _db.AuditTrail
            .Select(a => a.Entity)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return new AuditViewModel
        {
            Q = q,
            Action = action,
            Entity = entity,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Entries = entries,
            Actions = actions,
            Entities = entities
        };
    }
}