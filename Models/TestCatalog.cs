using System.ComponentModel.DataAnnotations.Schema;

namespace ZDLISPlus.Web.Models;

public class TestCatalog
{
    public int Id { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string SampleType { get; set; } = "";
    public int TurnaroundMinutes { get; set; } = 30;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // ---- NEW: profile / component relationship ----
    public int? ParentTestId { get; set; }
    public TestCatalog? ParentTest { get; set; }
    public List<TestCatalog> Children { get; set; } = new();

    public bool IsProfile { get; set; }             // CBC, Lipid Profile, Urinalysis
    public bool IsOrderable { get; set; } = true;   // can be picked in New Order

    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public int SortOrder { get; set; }

    public string DisplayLabel =>
        IsProfile ? $"{Code} — {Name} (profile)"
                  : (ParentTest is null ? $"{Code} — {Name}"
                                        : $"{ParentTest.Code} › {Code} — {Name}");
}