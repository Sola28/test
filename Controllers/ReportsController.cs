using Microsoft.AspNetCore.Mvc;
using ZDLISPlus.Web.Authorization;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Authorization;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Reports")]
public class ReportsController : Controller
{
    private readonly LabDataService _lab;
    public ReportsController(LabDataService lab) => _lab = lab;

    public async Task<IActionResult> Index(int days = 7) => View(await _lab.GetReportsAsync(days));
}