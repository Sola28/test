using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public class ProtocolParserFactory
{
    public IProtocolParser Create(ProtocolKind kind, string? customPattern = null)
        => kind switch
        {
            ProtocolKind.HL7 => new Hl7Parser(),
            ProtocolKind.ASTM => new AstmParser(),
            ProtocolKind.Custom => new CustomRegexParser(customPattern ?? ""),
            _ => new Hl7Parser()
        };
}