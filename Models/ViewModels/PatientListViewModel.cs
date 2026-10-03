namespace ZDLISPlus.Web.Models.ViewModels;

public class PatientListViewModel
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public List<Patient> Patients { get; set; } = new();

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}