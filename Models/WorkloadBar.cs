namespace ZDLISPlus.Web.Models;

public class WorkloadBar
{
    public string Hour { get; set; } = "";
    public int Value { get; set; }
    public bool IsPeak { get; set; }
}