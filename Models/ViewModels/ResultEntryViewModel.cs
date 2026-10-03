namespace ZDLISPlus.Web.Models.ViewModels;

public class ResultEntryViewModel
{
    public int OrderId { get; set; }
    public string Accession { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientMrn { get; set; } = "";
    public string PatientGender { get; set; } = "";
    public int PatientAge { get; set; }
    public string OrderType { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string StatusCss { get; set; } = "";
    public DateTime OrderedAt { get; set; }
    public string? RequestingPhysician { get; set; }

    public List<ResultEntryGroup> Groups { get; set; } = new();
}

public class ResultEntryGroup
{
    public int OrderedTestId { get; set; }
    public string OrderedTestCode { get; set; } = "";
    public string OrderedTestName { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string SampleType { get; set; } = "";
    public bool IsProfile { get; set; }
    public List<ResultEntryRow> Rows { get; set; } = new();
}

public class ResultEntryRow
{
    public int ComponentTestId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Value { get; set; }
    public string? Flag { get; set; }
}