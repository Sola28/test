using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models.ViewModels;

public class InstrumentEditViewModel
{
    public int? Id { get; set; }

    [Required] public string Name { get; set; } = "";
    [Required] public string Discipline { get; set; } = "Chemistry";
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }

    public InterfaceKind InterfaceKind { get; set; } = InterfaceKind.TcpServer;
    public ProtocolKind ProtocolKind { get; set; } = ProtocolKind.HL7;

    public string? TcpHost { get; set; }
    public int? TcpPort { get; set; }

    public string? SerialPort { get; set; }
    public int? SerialBaud { get; set; } = 9600;
    public int? SerialDataBits { get; set; } = 8;
    public string? SerialParity { get; set; } = "None";
    public string? SerialStopBits { get; set; } = "One";

    public string? WatchFolder { get; set; }

    public bool IsEnabled { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public string? Notes { get; set; }

    public List<string> Disciplines { get; set; } = new();
}