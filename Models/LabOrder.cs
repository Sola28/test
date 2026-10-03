namespace ZDLISPlus.Web.Models;

public enum OrderPriority { Normal, Urgent }
public enum OrderStatus { Collected, Processing, ForValidation, Released, Critical }

public class LabOrder
{
    public int Id { get; set; }
    public string Accession { get; set; } = "";
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string OrderType { get; set; } = "Outpatient";
    public DateTime OrderedAt { get; set; } = DateTime.Now;
    public string? RequestingPhysician { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Collected;
    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    public List<OrderTest> Tests { get; set; } = new();

    public string StatusCss => Status switch
    {
        OrderStatus.ForValidation => "amber",
        OrderStatus.Processing => "blue",
        OrderStatus.Released => "green",
        OrderStatus.Critical => "red",
        _ => "slate"
    };

    public string StatusLabel => Status switch
    {
        OrderStatus.ForValidation => "For validation",
        OrderStatus.Processing => "Processing",
        OrderStatus.Released => "Released",
        OrderStatus.Critical => "Critical",
        OrderStatus.Collected => "Collected",
        _ => Status.ToString()
    };
}