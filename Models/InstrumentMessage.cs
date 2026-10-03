namespace ZDLISPlus.Web.Models;

public enum MessageDirection { Inbound, Outbound }
public enum MessageStatus { Received, Parsed, Dispatched, Error }

public class InstrumentMessage
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public DateTime At { get; set; } = DateTime.Now;
    public MessageDirection Direction { get; set; } = MessageDirection.Inbound;
    public MessageStatus Status { get; set; } = MessageStatus.Received;

    /// <summary>Raw text — HL7 message, ASTM record, or file contents.</summary>
    public string Raw { get; set; } = "";

    /// <summary>Extracted accession/sample id if the parser could find one.</summary>
    public string? SampleId { get; set; }

    /// <summary>Human-readable summary or error message.</summary>
    public string? Summary { get; set; }

    /// <summary>Number of test results extracted from this message.</summary>
    public int ResultCount { get; set; }
}