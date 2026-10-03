using Microsoft.AspNetCore.Mvc;
using ZDLISPlus.Web.Authorization;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Audit")]
public class AuditController : Controller
{
    private readonly LabDataService _lab;
    public AuditController(LabDataService lab) => _lab = lab;

    public async Task<IActionResult> Index(
        string? q,
        string? action,
        string? entity,
        DateTime? from,
        DateTime? to,
        int page = 1)
    {
        var vm = await _lab.GetAuditAsync(q, action, entity, from, to, page, 50);
        return View(vm);
    }
}