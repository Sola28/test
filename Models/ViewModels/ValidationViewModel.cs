namespace ZDLISPlus.Web.Models.ViewModels;

public class ValidationViewModel
{
    public List<LabOrder> Pending { get; set; } = new();
    public List<LabOrder> RecentlyValidated { get; set; } = new();
}