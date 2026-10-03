using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class NewOrderViewModel
{
    [Required, Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required, Display(Name = "Order type")]
    public string OrderType { get; set; } = "Outpatient";

    [Display(Name = "Priority")]
    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    [Display(Name = "Requesting physician")]
    public string? RequestingPhysician { get; set; }

    [Display(Name = "Tests")]
    public List<int> SelectedTestIds { get; set; } = new();

    public List<Patient> Patients { get; set; } = new();
    public List<TestCatalog> Tests { get; set; } = new();
}