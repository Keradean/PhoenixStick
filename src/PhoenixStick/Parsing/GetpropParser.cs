using System.Text.RegularExpressions;

namespace PhoenixStick.Parsing;

/// <summary>Liest die Ausgabe von <c>getprop</c>: eine Zeile pro Eigenschaft, "[schluessel]: [wert]".</summary>
public static partial class GetpropParser
{
    [GeneratedRegex(@"^\[(?<key>[^\]]+)\]: \[(?<value>.*)\]$")]
    private static partial Regex LineRegex();

    public static IReadOnlyDictionary<string, string> Parse(string output)
    {
        var props = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in output.Split('\n'))
        {
            var match = LineRegex().Match(rawLine.TrimEnd('\r'));
            if (match.Success)
                props[match.Groups["key"].Value] = match.Groups["value"].Value;
        }
        return props;
    }
}
