using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class TestCatalogEditViewModel
{
    public int? Id { get; set; }

    [Required, Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required, Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required, Display(Name = "Discipline")]
    public string Discipline { get; set; } = "";

    [Display(Name = "Sample type")]
    public string SampleType { get; set; } = "";

    [Range(1, 1440), Display(Name = "Turnaround (min)")]
    public int TurnaroundMinutes { get; set; } = 30;

    [Range(0, 999999), Display(Name = "Price")]
    public decimal Price { get; set; }

    [Display(Name = "Is a profile (panel)")]
    public bool IsProfile { get; set; }

    [Display(Name = "Orderable")]
    public bool IsOrderable { get; set; } = true;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Unit")]
    public string? Unit { get; set; }

    [Display(Name = "Reference range")]
    public string? ReferenceRange { get; set; }

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }

    // Set when the user is adding/editing a child
    public int? ParentTestId { get; set; }
    public string? ParentName { get; set; }

    public List<string> Disciplines { get; set; } = new();
}