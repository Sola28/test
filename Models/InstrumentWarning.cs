namespace ZDLISPlus.Web.Models;

public class InstrumentWarning
{
    public int Id { get; set; }
    public string Protocol { get; set; } = "FujiNx700"; // or HL7, ASTM
    public int Position { get; set; }                   // 1..11
    public string Symbol { get; set; } = "";            // H, L, @, #, $, +, -, *, ?, &, E, ¥
    public string Meaning { get; set; } = "";
    public bool BlocksAutoRelease { get; set; }         // true for E, ¥, $, +, -, *, ?
}