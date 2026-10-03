using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Models.ViewModels;

public class ReportsViewModel
{
    public int Days { get; set; } = 7;
    public List<WorkloadBar> DailyVolume { get; set; } = new();
    public List<(string Discipline, int Count)> ByDiscipline { get; set; } = new();
    public List<LabOrder> Released { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public double AverageTat { get; set; }
}