using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Authorization;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Results")]
public class ResultsController : Controller
{
    private readonly LabDbContext _db;
    private readonly OrderService _orders;

    public ResultsController(LabDbContext db, OrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest)
            .Where(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.ForValidation);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o => o.Accession.Contains(q) || o.Patient.FullName.Contains(q));

        return View(await query.OrderByDescending(o => o.OrderedAt).ToListAsync());
    }

    // ---- printable report ----
    public async Task<IActionResult> Print(int id)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.Tests).ThenInclude(t => t.CatalogTest).ThenInclude(c => c.Children)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var results = await _db.OrderTestResults
            .Where(r => r.OrderId == id)
            .ToDictionaryAsync(r => (r.OrderedTestId, r.ComponentTestId));

        ViewData["Results"] = results;

        _db.AuditTrail.Add(new AuditEntry
        {
            User = User.Identity?.Name ?? "anonymous",
            Action = "Print",
            Entity = "LabOrder",
            EntityKey = order.Accession,
            Details = $"Result report printed for {order.Patient.FullName}"
        });
        await _db.SaveChangesAsync();

        return View(order);
    }

    // ---- result entry ----
    [HttpGet]
    public async Task<IActionResult> Enter(int id)
    {
        var vm = await _orders.GetResultEntryAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int id, IFormCollection form)
    {
        await _orders.SaveResultsAsync(id, form, User.Identity?.Name);
        TempData["Success"] = "Results saved.";
        return RedirectToAction(nameof(Enter), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        await _orders.SubmitForValidationAsync(id, User.Identity?.Name);
        TempData["Success"] = "Submitted for validation.";
        return RedirectToAction(nameof(Index));
    }
}