using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZDLISPlus.Web.Models.ViewModels;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _users;
    private readonly AuditService _audit;

    public AccountController(
        SignInManager<IdentityUser> signIn,
        UserManager<IdentityUser> users,
        AuditService audit)
    {
        _signIn = signIn;
        _users = users;
        _audit = audit;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _users.FindByEmailAsync(vm.Email);
        if (user is null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            await _audit.LogAsync("LoginFailed", "User", vm.Email, "Unknown email");
            return View(vm);
        }

        var result = await _signIn.PasswordSignInAsync(
            user.UserName!, vm.Password, vm.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _audit.LogAsync("Login", "User", user.UserName!, "Successful sign-in");
            return RedirectToLocal(vm.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            await _audit.LogAsync("LoginLockedOut", "User", user.UserName!, "Account locked");
            ModelState.AddModelError("", "Account is locked. Try again later.");
            return View(vm);
        }

        await _audit.LogAsync("LoginFailed", "User", user.UserName!, "Bad password");
        ModelState.AddModelError("", "Invalid email or password.");
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var name = User.Identity?.Name ?? "unknown";
        await _signIn.SignOutAsync();
        await _audit.LogAsync("Logout", "User", name, "Signed out");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}