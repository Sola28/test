namespace ZDLISPlus.Web.Models;

public class AuditEntry
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string EntityKey { get; set; } = "";
    public string? Details { get; set; }
}