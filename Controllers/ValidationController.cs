using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Authorization;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Validation")]
public class ValidationController : Controller
{
    private readonly OrderService _orders;
    private readonly LabDbContext _db;

    public ValidationController(OrderService orders, LabDbContext db)
    {
        _orders = orders;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _orders.GetValidationQueueAsync();

        // Load all result rows for pending + recently validated orders
        var ids = vm.Pending.Select(o => o.Id)
            .Concat(vm.RecentlyValidated.Select(o => o.Id))
            .ToList();

        var results = await _db.OrderTestResults
            .Where(r => ids.Contains(r.OrderId))
            .ToDictionaryAsync(r => (r.OrderId, r.OrderedTestId, r.ComponentTestId));

        ViewData["Results"] = results;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id)
    {
        await _orders.UpdateStatusAsync(id, OrderStatus.Released, User.Identity?.Name);
        TempData["Success"] = "Result released.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FlagCritical(int id)
    {
        await _orders.UpdateStatusAsync(id, OrderStatus.Critical, User.Identity?.Name);
        TempData["Success"] = "Result flagged as critical.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        await _orders.UpdateStatusAsync(id, OrderStatus.Processing, User.Identity?.Name);
        _db.AuditTrail.Add(new AuditEntry
        {
            User = User.Identity?.Name ?? "system",
            Action = "RejectValidation",
            Entity = "LabOrder",
            EntityKey = id.ToString(),
            Details = reason ?? "Returned to result entry"
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Returned to result entry.";
        return RedirectToAction(nameof(Index));
    }
}