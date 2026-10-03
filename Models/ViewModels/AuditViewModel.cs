namespace ZDLISPlus.Web.Models.ViewModels;

public class AuditViewModel
{
    // filters
    public string? Q { get; set; }
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // paging
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    // data
    public List<AuditEntry> Entries { get; set; } = new();

    // dropdown sources
    public List<string> Actions { get; set; } = new();
    public List<string> Entities { get; set; } = new();
}