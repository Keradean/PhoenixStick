namespace PhoenixStick.Parsing;

/// <summary>Liest die Ausgabe von <c>settings list &lt;tabelle&gt;</c>: "schluessel=wert" pro Zeile.</summary>
public static class SettingsParser
{
    public static IReadOnlyDictionary<string, string> Parse(string output)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        string? lastKey = null;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var separator = line.IndexOf('=');

            if (separator > 0)
            {
                lastKey = line[..separator];
                settings[lastKey] = line[(separator + 1)..];
            }
            else if (lastKey is not null && line.Length > 0)
            {
                // Werte können Zeilenumbrüche enthalten, dann gehört die Zeile zum vorigen Schlüssel
                settings[lastKey] += "\n" + line;
            }
        }
        return settings;
    }
}
