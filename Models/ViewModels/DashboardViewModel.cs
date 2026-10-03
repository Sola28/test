using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Models.ViewModels;

public class DashboardViewModel
{
    public string TodayLabel { get; set; } = "";
    public List<StatCard> Stats { get; set; } = new();
    public List<WorkloadBar> Workload { get; set; } = new();
    public List<Instrument> Instruments { get; set; } = new();
    public List<LabOrder> RecentOrders { get; set; } = new();
    public int CompletedToday { get; set; }
    public int InProgressToday { get; set; }
    public int AverageTatMinutes { get; set; }
}