using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required, Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = false;

    public string? ReturnUrl { get; set; }
}