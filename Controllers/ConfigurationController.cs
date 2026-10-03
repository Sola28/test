using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZDLISPlus.Web.Authorization;
using ZDLISPlus.Web.Data;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Models.ViewModels;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Configuration")]
public class ConfigurationController : Controller
{
    private readonly LabDbContext _db;
    public ConfigurationController(LabDbContext db) => _db = db;

    private async Task<List<string>> DisciplinesAsync() =>
        await _db.TestCatalog
            .Where(t => t.ParentTestId == null)
            .Select(t => t.Discipline)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

    // ---------------- LIST ----------------
    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.TestCatalog
            .Include(t => t.Children.OrderBy(c => c.SortOrder))
            .Where(t => t.ParentTestId == null);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Code.Contains(q) || t.Name.Contains(q));

        var items = await query
            .OrderBy(t => t.Discipline)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Code)
            .ToListAsync();

        ViewData["Q"] = q;
        return View(items);
    }

    // ---------------- CREATE ----------------
    [HttpGet]
    public async Task<IActionResult> Create(int? parentId)
    {
        var vm = new TestCatalogEditViewModel
        {
            IsOrderable = parentId == null,
            IsProfile = false,
            Disciplines = await DisciplinesAsync(),
            ParentTestId = parentId
        };

        if (parentId is not null)
        {
            var parent = await _db.TestCatalog.FindAsync(parentId.Value);
            if (parent is null) return NotFound();
            vm.ParentName = $"{parent.Code} — {parent.Name}";
            vm.Discipline = parent.Discipline;
            vm.SampleType = parent.SampleType;
            vm.IsOrderable = false;
            ViewData["IsChild"] = true;
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TestCatalogEditViewModel vm)
    {
        // Guard: a profile can't be a child; a child must have a parent
        if (vm.ParentTestId is not null && vm.IsProfile)
            ModelState.AddModelError(nameof(vm.IsProfile), "A component cannot be a profile.");

        // Uniqueness check: code + parent scope
        var duplicate = await _db.TestCatalog.AnyAsync(t =>
            t.Code == vm.Code && t.ParentTestId == vm.ParentTestId);
        if (duplicate)
            ModelState.AddModelError(nameof(vm.Code), "A test with this code already exists in the same scope.");

        if (!ModelState.IsValid)
        {
            vm.Disciplines = await DisciplinesAsync();
            return View(vm);
        }

        var entity = new TestCatalog
        {
            Code = vm.Code.Trim().ToUpperInvariant(),
            Name = vm.Name.Trim(),
            Discipline = vm.Discipline.Trim(),
            SampleType = vm.SampleType.Trim(),
            TurnaroundMinutes = vm.TurnaroundMinutes,
            Price = vm.Price,
            IsProfile = vm.ParentTestId is null && vm.IsProfile,
            IsOrderable = vm.ParentTestId is null && vm.IsOrderable,
            IsActive = vm.IsActive,
            Unit = vm.Unit,
            ReferenceRange = vm.ReferenceRange,
            SortOrder = vm.SortOrder,
            ParentTestId = vm.ParentTestId
        };

        _db.TestCatalog.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = vm.ParentTestId is null
            ? $"Test {entity.Code} created."
            : $"Component {entity.Code} added to {vm.ParentName}.";

        return RedirectToAction(nameof(Index));
    }

    // ---------------- EDIT ----------------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.TestCatalog
            .Include(x => x.ParentTest)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        var vm = new TestCatalogEditViewModel
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Discipline = t.Discipline,
            SampleType = t.SampleType,
            TurnaroundMinutes = t.TurnaroundMinutes,
            Price = t.Price,
            IsProfile = t.IsProfile,
            IsOrderable = t.IsOrderable,
            IsActive = t.IsActive,
            Unit = t.Unit,
            ReferenceRange = t.ReferenceRange,
            SortOrder = t.SortOrder,
            ParentTestId = t.ParentTestId,
            ParentName = t.ParentTest is null ? null : $"{t.ParentTest.Code} — {t.ParentTest.Name}",
            Disciplines = await DisciplinesAsync()
        };
        ViewData["IsChild"] = t.ParentTestId is not null;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TestCatalogEditViewModel vm)
    {
        if (vm.Id is null) return BadRequest();

        var entity = await _db.TestCatalog.FindAsync(vm.Id.Value);
        if (entity is null) return NotFound();

        var duplicate = await _db.TestCatalog.AnyAsync(t =>
            t.Id != entity.Id && t.Code == vm.Code && t.ParentTestId == entity.ParentTestId);
        if (duplicate)
            ModelState.AddModelError(nameof(vm.Code), "A test with this code already exists in the same scope.");

        if (!ModelState.IsValid)
        {
            vm.Disciplines = await DisciplinesAsync();
            vm.ParentTestId = entity.ParentTestId;
            return View(vm);
        }

        entity.Code = vm.Code.Trim().ToUpperInvariant();
        entity.Name = vm.Name.Trim();
        entity.Discipline = vm.Discipline.Trim();
        entity.SampleType = vm.SampleType.Trim();
        entity.TurnaroundMinutes = vm.TurnaroundMinutes;
        entity.Price = vm.Price;
        entity.IsActive = vm.IsActive;
        entity.Unit = vm.Unit;
        entity.ReferenceRange = vm.ReferenceRange;
        entity.SortOrder = vm.SortOrder;

        // Only top-level rows can toggle profile/orderable flags
        if (entity.ParentTestId is null)
        {
            entity.IsProfile = vm.IsProfile;
            entity.IsOrderable = vm.IsOrderable;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Test {entity.Code} updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- DELETE ----------------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.TestCatalog
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        var usedInOrders = await _db.OrderTests.AnyAsync(o => o.CatalogTestId == id);
        if (usedInOrders)
        {
            TempData["Error"] = $"Cannot delete {t.Code} — it is used by existing orders. Mark it inactive instead.";
            return RedirectToAction(nameof(Index));
        }

        if (t.IsProfile && t.Children.Any())
        {
            _db.TestCatalog.RemoveRange(t.Children);
        }

        _db.TestCatalog.Remove(t);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Test {t.Code} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- REORDER CHILDREN ----------------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveChild(int id, string direction)
    {
        var child = await _db.TestCatalog.FindAsync(id);
        if (child?.ParentTestId is null) return NotFound();

        var siblings = await _db.TestCatalog
            .Where(c => c.ParentTestId == child.ParentTestId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync();

        var idx = siblings.FindIndex(c => c.Id == child.Id);
        var swapWith = direction == "up" ? idx - 1 : idx + 1;
        if (swapWith < 0 || swapWith >= siblings.Count)
            return RedirectToAction(nameof(Index));

        // Rebuild sort order after the swap
        (siblings[idx], siblings[swapWith]) = (siblings[swapWith], siblings[idx]);
        for (int i = 0; i < siblings.Count; i++)
            siblings[i].SortOrder = i + 1;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}