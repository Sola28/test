using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class UserCreateViewModel
{
    [Required, Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    public List<string> SelectedRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
}