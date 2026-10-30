using System.Text.RegularExpressions;

namespace PhoenixStick.Parsing;

/// <summary>Liest aus <c>dumpsys appops</c>, welche Pakete nicht im Hintergrund laufen dürfen.</summary>
public static partial class AppOpsParser
{
    [GeneratedRegex(@"^\s*Package (\S+):$")]
    private static partial Regex PackageLine();

    // Modus 1 = ignore, 2 = errored. 0 = allow, 3 = default
    [GeneratedRegex(@"^\s*RUN_IN_BACKGROUND: mode=([12])\b")]
    private static partial Regex RestrictedLine();

    public static IReadOnlySet<string> BackgroundRestricted(string output)
    {
        var restricted = new HashSet<string>(StringComparer.Ordinal);
        string? currentPackage = null;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Ops direkt unter "Uid ...:" gelten für die UID, nicht für das vorige Paket
            if (line.TrimStart().StartsWith("Uid ", StringComparison.Ordinal))
            {
                currentPackage = null;
                continue;
            }

            var package = PackageLine().Match(line);
            if (package.Success)
            {
                currentPackage = package.Groups[1].Value;
                continue;
            }

            if (currentPackage is not null && RestrictedLine().IsMatch(line))
                restricted.Add(currentPackage);
        }
        return restricted;
    }
}
