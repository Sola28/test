namespace ZDLISPlus.Web.Models;

public class OrderTest
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public LabOrder Order { get; set; } = null!;
    public int CatalogTestId { get; set; }
    public TestCatalog CatalogTest { get; set; } = null!;
    public string? Result { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Flag { get; set; }   // H, L, N
    public string? ValidatedBy { get; set; }
    public DateTime? ValidatedAt { get; set; }
}