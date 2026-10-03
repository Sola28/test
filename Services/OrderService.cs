using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;

namespace ZDLISPlus.Web.Services;

public class OrderService
{
    private readonly LabDbContext _db;
    private readonly PatientService _patients;
    public OrderService(LabDbContext db, PatientService patients) => (_db, _patients) = (db, patients);

    public async Task<OrderListViewModel> ListAsync(string? q, OrderStatus? status, int page, int pageSize)
    {
        var query = _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o =>
                o.Accession.Contains(q) ||
                o.Patient.FullName.Contains(q));

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.OrderedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new OrderListViewModel
        {
            Q = q,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Orders = items
        };
    }

    public Task<LabOrder?> GetAsync(int id) =>
        _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<LabOrder> CreateAsync(NewOrderViewModel vm)
    {
        var order = new LabOrder
        {
            Accession = await NextAccessionAsync(),
            PatientId = vm.PatientId,
            OrderType = vm.OrderType,
            Priority = vm.Priority,
            RequestingPhysician = vm.RequestingPhysician,
            OrderedAt = DateTime.Now,
            Status = OrderStatus.Collected
        };

        // Only allow IsOrderable catalog rows to be added directly.
        var allowed = await _db.TestCatalog
            .Where(t => vm.SelectedTestIds.Contains(t.Id) && t.IsOrderable && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var id in allowed)
            order.Tests.Add(new OrderTest { CatalogTestId = id });

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status, string? user = null)
    {
        var order = await _db.LabOrders.FindAsync(orderId);
        if (order is null) return;
        order.Status = status;

        if (status == OrderStatus.Released || status == OrderStatus.Critical)
        {
            var rows = await _db.OrderTestResults
                .Where(r => r.OrderId == orderId)
                .ToListAsync();
            foreach (var r in rows)
            {
                r.VerifiedBy ??= user;
                r.VerifiedAt ??= DateTime.Now;
            }
        }

        // If returning to entry, clear the verified stamp
        if (status == OrderStatus.Processing)
        {
            var rows = await _db.OrderTestResults.Where(r => r.OrderId == orderId).ToListAsync();
            foreach (var r in rows)
            {
                r.VerifiedBy = null;
                r.VerifiedAt = null;
            }
        }

        _db.AuditTrail.Add(new AuditEntry
        {
            User = user ?? "system",
            Action = $"StatusChange:{status}",
            Entity = "LabOrder",
            EntityKey = order.Accession,
            Details = $"Status changed to {status}"
        });

        await _db.SaveChangesAsync();
    }

    public async Task<ValidationViewModel> GetValidationQueueAsync()
    {
        var pending = await _db.LabOrders
            .Include(o => o.Patient).Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .Where(o => o.Status == OrderStatus.ForValidation)
            .OrderBy(o => o.OrderedAt)
            .ToListAsync();

        var recent = await _db.LabOrders
            .Include(o => o.Patient).Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .Where(o => o.Status == OrderStatus.Released || o.Status == OrderStatus.Critical)
            .OrderByDescending(o => o.OrderedAt)
            .Take(15)
            .ToListAsync();

        return new ValidationViewModel { Pending = pending, RecentlyValidated = recent };
    }

    private async Task<string> NextAccessionAsync()
    {
        var prefix = $"ZL-{DateTime.Today:yyMMdd}-";
        var last = await _db.LabOrders
            .Where(o => o.Accession.StartsWith(prefix))
            .OrderByDescending(o => o.Accession)
            .Select(o => o.Accession)
            .FirstOrDefaultAsync();

        var n = 1;
        if (last is not null && int.TryParse(last[^3..], out var parsed)) n = parsed + 1;
        return $"{prefix}{n:D3}";
    }
    public async Task<ResultEntryViewModel?> GetResultEntryAsync(int orderId)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest).ThenInclude(c => c.Children)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return null;

        var existing = await _db.OrderTestResults
            .Where(r => r.OrderId == orderId)
            .ToDictionaryAsync(r => (r.OrderedTestId, r.ComponentTestId));

        var vm = new ResultEntryViewModel
        {
            OrderId = order.Id,
            Accession = order.Accession,
            PatientName = order.Patient.FullName,
            PatientMrn = order.Patient.MedicalRecordNumber,
            PatientGender = order.Patient.Gender,
            PatientAge = order.Patient.Age,
            OrderType = order.OrderType,
            StatusLabel = order.StatusLabel,
            StatusCss = order.StatusCss,
            OrderedAt = order.OrderedAt,
            RequestingPhysician = order.RequestingPhysician
        };

        foreach (var ot in order.Tests)
        {
            var group = new ResultEntryGroup
            {
                OrderedTestId = ot.CatalogTestId,
                OrderedTestCode = ot.CatalogTest.Code,
                OrderedTestName = ot.CatalogTest.Name,
                Discipline = ot.CatalogTest.Discipline,
                SampleType = ot.CatalogTest.SampleType,
                IsProfile = ot.CatalogTest.IsProfile
            };

            var components = ot.CatalogTest.IsProfile && ot.CatalogTest.Children.Any()
                ? ot.CatalogTest.Children.OrderBy(c => c.SortOrder).ToList()
                : new List<TestCatalog> { ot.CatalogTest };

            foreach (var c in components)
            {
                existing.TryGetValue((ot.CatalogTestId, c.Id), out var saved);

                group.Rows.Add(new ResultEntryRow
                {
                    ComponentTestId = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Unit = c.Unit,
                    ReferenceRange = c.ReferenceRange,
                    Value = saved?.Value,
                    Flag = saved?.Flag
                });
            }

            vm.Groups.Add(group);
        }

        return vm;
    }

    public async Task SaveResultsAsync(int orderId, IFormCollection form, string? user)
    {
        var order = await _db.LabOrders
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest).ThenInclude(c => c.Children)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Order not found");

        var existing = await _db.OrderTestResults
            .Where(r => r.OrderId == orderId)
            .ToListAsync();

        foreach (var ot in order.Tests)
        {
            var components = ot.CatalogTest.IsProfile && ot.CatalogTest.Children.Any()
                ? ot.CatalogTest.Children
                : new List<TestCatalog> { ot.CatalogTest };

            foreach (var c in components)
            {
                var key = $"v_{ot.CatalogTestId}_{c.Id}";
                var raw = form[key].ToString();
                var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
                var flag = ResultFlagService.ComputeFlag(value, c.ReferenceRange);

                var row = existing.FirstOrDefault(r =>
                    r.OrderedTestId == ot.CatalogTestId && r.ComponentTestId == c.Id);

                if (row is null)
                {
                    row = new OrderTestResult
                    {
                        OrderId = orderId,
                        OrderedTestId = ot.CatalogTestId,
                        ComponentTestId = c.Id
                    };
                    _db.OrderTestResults.Add(row);
                }

                row.Value = value;
                row.Flag = flag;
                row.EnteredBy = user;
                row.EnteredAt = DateTime.Now;
            }
        }

        // Bump status to Processing when there is at least one value
        if (order.Status == OrderStatus.Collected)
            order.Status = OrderStatus.Processing;

        _db.AuditTrail.Add(new AuditEntry
        {
            User = user ?? "system",
            Action = "ResultEntry",
            Entity = "LabOrder",
            EntityKey = order.Accession,
            Details = "Result values saved"
        });

        await _db.SaveChangesAsync();
    }

    public async Task SubmitForValidationAsync(int orderId, string? user)
    {
        var order = await _db.LabOrders.FindAsync(orderId);
        if (order is null) return;
        order.Status = OrderStatus.ForValidation;
        _db.AuditTrail.Add(new AuditEntry
        {
            User = user ?? "system",
            Action = "SubmitForValidation",
            Entity = "LabOrder",
            EntityKey = order.Accession,
            Details = "Results submitted to validation queue"
        });
        await _db.SaveChangesAsync();
    }
}