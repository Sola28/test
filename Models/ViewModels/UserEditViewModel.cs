using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class UserEditViewModel
{
    [Required]
    public string Id { get; set; } = "";

    [Required, Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    public bool IsLockedOut { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsLastAdmin { get; set; }

    public List<string> SelectedRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();

    // Optional password reset — if both are set, password changes.
    [DataType(DataType.Password), Display(Name = "New password")]
    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}