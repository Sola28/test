namespace ZDLISPlus.Web.Models;

public class InstrumentLog
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
    public string Level { get; set; } = "Info";   // Info, Warn, Error
    public string Message { get; set; } = "";
}