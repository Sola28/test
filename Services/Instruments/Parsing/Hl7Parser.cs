using System.Text.RegularExpressions;

namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public class Hl7Parser : IProtocolParser
{
    public string Name => "HL7 v2.x";

    public ParseOutcome Parse(string raw)
    {
        var outcome = new ParseOutcome();
        if (string.IsNullOrWhiteSpace(raw))
        {
            outcome.Error = "Empty message.";
            return outcome;
        }

        // Normalise line endings
        var lines = raw.Replace("\r\n", "\r").Replace("\n", "\r")
                       .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        string fieldSep = "|";
        string componentSep = "^";
        string repeatSep = "~";

        foreach (var line in lines)
        {
            if (line.StartsWith("MSH"))
            {
                fieldSep = line.Substring(3, 1);
                if (line.Length > 4) componentSep = line.Substring(4, 1);
                if (line.Length > 5) repeatSep = line.Substring(5, 1);
            }
            else if (line.StartsWith("PID"))
            {
                var f = line.Split(fieldSep);
                // PID-3 = patient identifier
                if (f.Length > 3 && !string.IsNullOrWhiteSpace(f[3]))
                    outcome.SampleId ??= f[3].Split(componentSep)[0];
            }
            else if (line.StartsWith("OBR"))
            {
                var f = line.Split(fieldSep);
                // OBR-3 = filler order number (usually the accession)
                if (f.Length > 3 && !string.IsNullOrWhiteSpace(f[3]))
                    outcome.SampleId = f[3].Split(componentSep)[0];
                else if (f.Length > 2 && !string.IsNullOrWhiteSpace(f[2]))
                    outcome.SampleId ??= f[2].Split(componentSep)[0];
            }
            else if (line.StartsWith("OBX"))
            {
                var f = line.Split(fieldSep);
                // OBX-3 = observation identifier (test code^name)
                if (f.Length > 5)
                {
                    var code = f[3].Split(componentSep)[0].Trim();
                    var value = f[5].Trim();
                    var unit = f.Length > 6 ? f[6].Trim() : null;
                    var refRange = f.Length > 7 ? f[7].Trim() : null;
                    var flag = f.Length > 8 ? f[8].Trim() : null;

                    if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(value))
                    {
                        outcome.Results.Add(new ParsedResult
                        {
                            SampleId = outcome.SampleId,
                            InstrumentCode = code,
                            Value = value,
                            Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                            ReferenceRange = string.IsNullOrWhiteSpace(refRange) ? null : refRange,
                            Flag = string.IsNullOrWhiteSpace(flag) ? null : flag,
                            ObservedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        outcome.Success = outcome.Results.Count > 0;
        if (!outcome.Success && string.IsNullOrEmpty(outcome.Error))
            outcome.Error = "No OBX results found in message.";

        return outcome;
    }
}