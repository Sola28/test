using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZDLISPlus.Web.Models;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Authorization;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Patients")]
public class PatientsController : Controller
{
    private readonly PatientService _svc;
    public PatientsController(PatientService svc) => _svc = svc;

    public async Task<IActionResult> Index(string? q, int page = 1)
        => View(await _svc.ListAsync(q, page, 10));

    public async Task<IActionResult> Details(int id)
    {
        var p = await _svc.GetAsync(id);
        if (p is null) return NotFound();
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient p)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please review the patient form.";
            return RedirectToAction(nameof(Index));
        }
        var created = await _svc.CreateAsync(p);
        TempData["Success"] = $"Patient {created.FullName} created ({created.MedicalRecordNumber}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Patient p)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please review the patient form.";
            return RedirectToAction(nameof(Index));
        }
        await _svc.UpdateAsync(p);
        TempData["Success"] = $"Patient {p.FullName} updated.";
        return RedirectToAction(nameof(Details), new { id = p.Id });
    }
}