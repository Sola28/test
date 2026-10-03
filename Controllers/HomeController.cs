using Microsoft.AspNetCore.Mvc;
using ZDLISPlus.Web.Services;
using ZDLISPlus.Web.Authorization;

namespace ZDLISPlus.Web.Controllers;

[ModuleAuthorize("Home")]
public class HomeController : Controller
{
    private readonly LabDataService _lab;
    public HomeController(LabDataService lab) => _lab = lab;

    public async Task<IActionResult> Index() => View(await _lab.GetDashboardAsync());
}