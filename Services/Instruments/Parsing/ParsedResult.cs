namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public class ParsedResult
{
    public string? SampleId { get; set; }
    public string InstrumentCode { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Flag { get; set; }
    public DateTime? ObservedAt { get; set; }
}

public class ParseOutcome
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SampleId { get; set; }
    public List<ParsedResult> Results { get; set; } = new();
}