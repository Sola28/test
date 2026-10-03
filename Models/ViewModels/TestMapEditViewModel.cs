namespace ZDLISPlus.Web.Models.ViewModels;

public class TestMapEditViewModel
{
    public int InstrumentId { get; set; }
    public string InstrumentName { get; set; } = "";
    public List<TestMapRow> Rows { get; set; } = new();
    public List<TestCatalog> AllTests { get; set; } = new();

    // New row to add
    public string? NewInstrumentCode { get; set; }
    public int? NewComponentTestId { get; set; }
    public decimal? NewFactor { get; set; }
    public string? NewUnitOverride { get; set; }
}

public class TestMapRow
{
    public int Id { get; set; }
    public string InstrumentCode { get; set; } = "";
    public int ComponentTestId { get; set; }
    public string ComponentLabel { get; set; } = "";
    public decimal? Factor { get; set; }
    public string? UnitOverride { get; set; }
    public bool IsActive { get; set; }
}