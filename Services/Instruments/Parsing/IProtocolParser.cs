namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public interface IProtocolParser
{
    string Name { get; }
    ParseOutcome Parse(string raw);
}