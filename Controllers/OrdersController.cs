using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Authorization;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Orders")]
public class OrdersController : Controller
{
    private readonly OrderService _orders;
    private readonly PatientService _patients;
    private readonly LabDbContext _db;

    public OrdersController(OrderService orders, PatientService patients, LabDbContext db)
        => (_orders, _patients, _db) = (orders, patients, db);

    public async Task<IActionResult> Index(string? q, OrderStatus? status, int page = 1)
        => View(await _orders.ListAsync(q, status, page, 15));

    public async Task<IActionResult> Details(int id)
    {
        var o = await _orders.GetAsync(id);
        if (o is null) return NotFound();
        return View(o);
    }

    [HttpGet]
    public async Task<IActionResult> New()
    {
        var vm = new NewOrderViewModel
        {
            Patients = (await _patients.ListAsync(null, 1, 200)).Patients,
            Tests = await _db.TestCatalog.Where(t => t.IsActive).OrderBy(t => t.Discipline).ThenBy(t => t.Name).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> New(NewOrderViewModel vm)
    {
        if (vm.SelectedTestIds.Count == 0)
            ModelState.AddModelError(nameof(vm.SelectedTestIds), "Select at least one test.");

        if (!ModelState.IsValid)
        {
            vm.Patients = (await _patients.ListAsync(null, 1, 200)).Patients;
            vm.Tests = await _db.TestCatalog
                .Include(t => t.Children)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Discipline).ThenBy(t => t.Name)
                .ToListAsync();
            return View(vm);
        }

        var order = await _orders.CreateAsync(vm);
        TempData["Success"] = $"Order {order.Accession} created.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        await _orders.UpdateStatusAsync(id, status, User.Identity?.Name);
        TempData["Success"] = $"Status set to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}