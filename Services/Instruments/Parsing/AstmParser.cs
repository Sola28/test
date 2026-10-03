namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public class AstmParser : IProtocolParser
{
    public string Name => "ASTM E1394";

    public ParseOutcome Parse(string raw)
    {
        var outcome = new ParseOutcome();
        if (string.IsNullOrWhiteSpace(raw))
        {
            outcome.Error = "Empty message.";
            return outcome;
        }

        // ASTM usually uses \x0D as record separator, but files/tests often use \r\n
        var records = raw.Replace("\r\n", "\r").Replace("\n", "\r")
                         .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        string fieldSep = "|";
        string componentSep = "^";

        foreach (var rec in records)
        {
            if (rec.Length < 2) continue;

            var type = rec.Substring(0, 1);
            var body = rec.Substring(1);
            if (body.StartsWith(fieldSep)) body = body.Substring(1);

            var f = body.Split(fieldSep);

            switch (type)
            {
                case "H":
                    if (f.Length > 1 && !string.IsNullOrEmpty(f[1]))
                        componentSep = f[1].Substring(0, 1);
                    break;

                case "P":
                    // P-3 = patient id
                    if (f.Length > 3 && !string.IsNullOrWhiteSpace(f[3]))
                        outcome.SampleId ??= f[3].Split(componentSep)[0];
                    break;

                case "O":
                    // O-3 = sample id
                    if (f.Length > 3 && !string.IsNullOrWhiteSpace(f[3]))
                        outcome.SampleId = f[3].Split(componentSep)[0];
                    break;

                case "R":
                    // R-2 = test code, R-3 = value, R-4 = unit, R-5 = reference range, R-6 = flag
                    if (f.Length > 3)
                    {
                        var code = f[2].Split(componentSep)[0].Trim();
                        var value = f[3].Trim();
                        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(value))
                        {
                            outcome.Results.Add(new ParsedResult
                            {
                                SampleId = outcome.SampleId,
                                InstrumentCode = code,
                                Value = value,
                                Unit = f.Length > 4 ? NullIfEmpty(f[4]) : null,
                                ReferenceRange = f.Length > 5 ? NullIfEmpty(f[5]) : null,
                                Flag = f.Length > 6 ? NullIfEmpty(f[6]) : null,
                                ObservedAt = DateTime.Now
                            });
                        }
                    }
                    break;
            }
        }

        outcome.Success = outcome.Results.Count > 0;
        if (!outcome.Success && string.IsNullOrEmpty(outcome.Error))
            outcome.Error = "No R (result) records found.";
        return outcome;
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}