using System.Text.RegularExpressions;

namespace ZDLISPlus.Web.Services.Instruments.Parsing;

public class CustomRegexParser : IProtocolParser
{
    private readonly string _pattern;
    public string Name => "Custom regex";

    public CustomRegexParser(string pattern) => _pattern = pattern;

    public ParseOutcome Parse(string raw)
    {
        var outcome = new ParseOutcome();
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(_pattern))
        {
            outcome.Error = "Empty message or pattern.";
            return outcome;
        }

        try
        {
            // Expect named groups: sample, code, value, unit, ref, flag
            var rx = new Regex(_pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match m in rx.Matches(raw))
            {
                outcome.SampleId ??= G(m, "sample");
                var code = G(m, "code");
                var value = G(m, "value");
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(value))
                    continue;

                outcome.Results.Add(new ParsedResult
                {
                    SampleId = outcome.SampleId,
                    InstrumentCode = code!,
                    Value = value!,
                    Unit = G(m, "unit"),
                    ReferenceRange = G(m, "ref"),
                    Flag = G(m, "flag"),
                    ObservedAt = DateTime.Now
                });
            }

            outcome.Success = outcome.Results.Count > 0;
            if (!outcome.Success) outcome.Error = "Pattern matched but produced no results.";
        }
        catch (Exception ex)
        {
            outcome.Error = "Regex error: " + ex.Message;
        }

        return outcome;
    }

    private static string? G(Match m, string name) =>
        m.Groups[name].Success && !string.IsNullOrWhiteSpace(m.Groups[name].Value)
            ? m.Groups[name].Value.Trim() : null;
}