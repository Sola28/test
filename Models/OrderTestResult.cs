namespace ZDLISPlus.Web.Models;

public class OrderTestResult
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public LabOrder Order { get; set; } = null!;

    // The profile/standalone row that was ordered (e.g. CBC)
    public int OrderedTestId { get; set; }
    public TestCatalog OrderedTest { get; set; } = null!;

    // The specific component being filled in. For standalone tests this equals OrderedTestId.
    public int ComponentTestId { get; set; }
    public TestCatalog ComponentTest { get; set; } = null!;

    public string? Value { get; set; }          // "13.5" or "Negative"
    public string? Flag { get; set; }           // H, L, N, or null
    public string? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}