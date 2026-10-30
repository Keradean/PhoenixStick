using System.Globalization;
using System.Text.RegularExpressions;

namespace PhoenixStick.Parsing;

public record ProcessCpu(string Name, int Pid, double Percent);

public record CpuInfo(double Load1, double Load5, double Load15, IReadOnlyList<ProcessCpu> Processes);

/// <summary>Liest die Ausgabe von <c>dumpsys cpuinfo</c>: Systemlast und CPU-Anteil pro Prozess.</summary>
public static partial class CpuinfoParser
{
    [GeneratedRegex(@"^Load: ([\d.]+) / ([\d.]+) / ([\d.]+)")]
    private static partial Regex LoadLine();

    [GeneratedRegex(@"^\s*\+?([\d.]+)% (\d+)/([^:]+):")]
    private static partial Regex ProcessLine();

    public static CpuInfo Parse(string output)
    {
        double load1 = 0, load5 = 0, load15 = 0;
        var processes = new List<ProcessCpu>();

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            var load = LoadLine().Match(line);
            if (load.Success)
            {
                load1 = ParseDouble(load.Groups[1].Value);
                load5 = ParseDouble(load.Groups[2].Value);
                load15 = ParseDouble(load.Groups[3].Value);
                continue;
            }

            var process = ProcessLine().Match(line);
            if (process.Success)
                processes.Add(new ProcessCpu(process.Groups[3].Value, int.Parse(process.Groups[2].Value), ParseDouble(process.Groups[1].Value)));
        }

        return new CpuInfo(load1, load5, load15, processes);
    }

    private static double ParseDouble(string value) => double.Parse(value, CultureInfo.InvariantCulture);
}
