using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ZDLISPlus.Web.Services;

namespace ZDLISPlus.Web.Authorization;

/// <summary>
/// Requires the current user to have access to a given module.
/// Administrators always pass. Non-admins must have at least one
/// role that is granted the module in RolePermission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ModuleAuthorizeAttribute : Attribute, IFilterFactory
{
    public string Module { get; }
    public bool IsReusable => false;

    public ModuleAuthorizeAttribute(string module) => Module = module;

    public IFilterMetadata CreateInstance(IServiceProvider sp)
        => new ModuleAuthorizeFilter(sp.GetRequiredService<PermissionService>(), Module);

    private sealed class ModuleAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly PermissionService _perm;
        private readonly string _module;

        public ModuleAuthorizeFilter(PermissionService perm, string module)
        {
            _perm = perm;
            _module = module;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            // If Identity already denied (e.g. user not authenticated),
            // don't override that decision.
            if (ctx.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                ctx.Result = new ChallengeResult();
                return;
            }

            if (await _perm.CanAccessAsync(ctx.HttpContext.User, _module))
                return;

            ctx.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}