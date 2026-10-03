namespace ZDLISPlus.Web.Models;

public enum InstrumentState
{
    Online,
    Idle,
    Offline,
    Error
}

public enum InterfaceKind
{
    TcpClient,
    TcpServer,
    Serial,
    File
}

public enum ProtocolKind
{
    HL7,
    ASTM,
    Custom
}

public class Instrument
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string SerialNumber { get; set; } = "";

    public InstrumentState State { get; set; } = InstrumentState.Offline;
    public DateTime? LastSeenAt { get; set; }
    public string QueueNote { get; set; } = "";

    // Interface
    public InterfaceKind InterfaceKind { get; set; } = InterfaceKind.TcpServer;
    public ProtocolKind ProtocolKind { get; set; } = ProtocolKind.HL7;

    // TCP
    public string? TcpHost { get; set; }
    public int? TcpPort { get; set; }

    // Serial
    public string? SerialPort { get; set; }
    public int? SerialBaud { get; set; } = 9600;
    public int? SerialDataBits { get; set; } = 8;
    public string? SerialParity { get; set; } = "None";
    public string? SerialStopBits { get; set; } = "One";

    // File
    public string? WatchFolder { get; set; }

    // Behaviour
    public bool IsEnabled { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation
    public List<InstrumentTestMap> TestMaps { get; set; } = new();
    public List<InstrumentMessage> Messages { get; set; } = new();

    public string StateCss => State.ToString().ToLowerInvariant();
}