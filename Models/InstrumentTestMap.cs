namespace ZDLISPlus.Web.Models;

/// <summary>
/// Maps an instrument's internal test code (e.g. "WBC", "HGB", "GLU")
/// to a component in the ZDLIS test catalog.
/// </summary>
public class InstrumentTestMap
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public string InstrumentCode { get; set; } = "";   // code the instrument sends
    public int ComponentTestId { get; set; }           // FK to TestCatalog
    public TestCatalog ComponentTest { get; set; } = null!;

    /// <summary>Optional override: some instruments send their own unit.</summary>
    public string? UnitOverride { get; set; }

    /// <summary>Optional decimal scaling factor (e.g. instrument sends 0.135, catalog expects 13.5).</summary>
    public decimal? Factor { get; set; }

    public bool IsActive { get; set; } = true;
}