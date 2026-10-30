using System.Text.RegularExpressions;

namespace PhoenixStick.Parsing;

public record ProcessMemory(string Name, int Pid, long PssKb);

public record MemoryInfo(long UptimeMs, long TotalKb, long FreeKb, long UsedKb, IReadOnlyList<ProcessMemory> Processes);

/// <summary>Liest die Ausgabe von <c>dumpsys meminfo</c> (ohne Argumente).</summary>
public static partial class MeminfoParser
{
    [GeneratedRegex(@"^\s*([\d,.]+)K: (\S+) \(pid (\d+)")]
    private static partial Regex ProcessLine();

    [GeneratedRegex(@"^\s*(Total|Free|Used) RAM:\s+([\d,.]+)K")]
    private static partial Regex RamLine();

    [GeneratedRegex(@"^Uptime: (\d+)")]
    private static partial Regex UptimeLine();

    public static MemoryInfo Parse(string output)
    {
        long uptime = 0, total = 0, free = 0, used = 0;
        var processes = new List<ProcessMemory>();
        var inProcessList = false;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith("Total PSS by process", StringComparison.Ordinal))
            {
                inProcessList = true;
                continue;
            }
            if (inProcessList)
            {
                // Die Prozessliste endet mit einer Leerzeile oder dem nächsten Abschnitt
                var match = ProcessLine().Match(line);
                if (match.Success)
                {
                    processes.Add(new ProcessMemory(match.Groups[2].Value, int.Parse(match.Groups[3].Value), ParseKb(match.Groups[1].Value)));
                    continue;
                }
                inProcessList = false;
            }

            var uptimeMatch = UptimeLine().Match(line);
            if (uptimeMatch.Success)
                uptime = long.Parse(uptimeMatch.Groups[1].Value);

            var ram = RamLine().Match(line);
            if (!ram.Success)
                continue;
            var value = ParseKb(ram.Groups[2].Value);
            switch (ram.Groups[1].Value)
            {
                case "Total": total = value; break;
                case "Free": free = value; break;
                case "Used": used = value; break;
            }
        }

        if (total == 0)
            throw new FormatException("dumpsys meminfo: \"Total RAM\" nicht gefunden");

        return new MemoryInfo(uptime, total, free, used, processes);
    }

    // Tausendertrennzeichen hängen vom Gerät ab ("1,316,676" oder "1.316.676")
    private static long ParseKb(string value) => long.Parse(value.Replace(",", "").Replace(".", ""));
}
