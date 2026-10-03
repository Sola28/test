namespace ZDLISPlus.Web.Models.ViewModels;

public class OrderListViewModel
{
    public string? Q { get; set; }
    public OrderStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public List<LabOrder> Orders { get; set; } = new();

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}