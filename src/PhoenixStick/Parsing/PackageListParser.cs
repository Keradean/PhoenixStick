namespace PhoenixStick.Parsing;

/// <summary>Liest die Ausgabe von <c>pm list packages</c>, mit oder ohne <c>-f</c>.</summary>
public static class PackageListParser
{
    private const string Prefix = "package:";

    public static IReadOnlyList<string> Parse(string output)
    {
        var packages = new List<string>();
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            var value = line[Prefix.Length..];

            // Mit -f: "package:/pfad/zur/base.apk=paket.name". Paketnamen enthalten kein "="
            var separator = value.LastIndexOf('=');
            if (separator >= 0)
                value = value[(separator + 1)..];

            if (value.Length > 0)
                packages.Add(value);
        }
        return packages;
    }
}
