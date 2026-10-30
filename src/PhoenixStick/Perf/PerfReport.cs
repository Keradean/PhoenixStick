using System.Text.Json;
using PhoenixStick.Adb;
using PhoenixStick.Parsing;
using PhoenixStick.Storage;

namespace PhoenixStick.Perf;

public record PerfReport(
    DateTime Timestamp,
    string Note,
    long UptimeMs,
    long TotalRamKb,
    long FreeRamKb,
    long UsedRamKb,
    int ProcessCount,
    double Load1,
    double Load5,
    double Load15,
    IReadOnlyList<ProcessMemory> TopMemory,
    IReadOnlyList<ProcessCpu> TopCpu);

/// <summary>Misst RAM und CPU über dumpsys und speichert die Messung unter state/perf/.</summary>
public class PerfRecorder(IAdbClient adb, StatePaths paths)
{
    public const int TopCount = 15;

    private readonly IAdbClient _adb = adb;
    private readonly StatePaths _paths = paths;

    private string PerfDir => Path.Combine(_paths.Root, "perf");

    public async Task<PerfReport> MeasureAsync(string note, DateTime now, CancellationToken ct = default)
    {
        var memory = MeminfoParser.Parse(await ReadAsync("dumpsys meminfo", ct));
        var cpu = CpuinfoParser.Parse(await ReadAsync("dumpsys cpuinfo", ct));

        return new PerfReport(
            now,
            note,
            memory.UptimeMs,
            memory.TotalKb,
            memory.FreeKb,
            memory.UsedKb,
            memory.Processes.Count,
            cpu.Load1,
            cpu.Load5,
            cpu.Load15,
            memory.Processes.OrderByDescending(p => p.PssKb).Take(TopCount).ToList(),
            cpu.Processes.Where(p => p.Name != "dumpsys").OrderByDescending(p => p.Percent).Take(TopCount).ToList());
    }

    /// <summary>Letzte gespeicherte Messung, null wenn es noch keine gibt.</summary>
    public PerfReport? LoadLatest()
    {
        if (!Directory.Exists(PerfDir))
            return null;

        var latest = Directory.GetFiles(PerfDir, "*.json").Order(StringComparer.Ordinal).LastOrDefault();
        return latest is null ? null : JsonSerializer.Deserialize<PerfReport>(File.ReadAllText(latest), JsonDefaults.Options);
    }

    public string Save(PerfReport report)
    {
        Directory.CreateDirectory(PerfDir);
        var file = Path.Combine(PerfDir, $"{StatePaths.Timestamp(report.Timestamp)}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(report, JsonDefaults.Options));
        return file;
    }

    private async Task<string> ReadAsync(string command, CancellationToken ct)
    {
        var result = await _adb.ShellReadAsync(command, ct);
        if (!result.Success)
            throw new AdbCommandException(command, result);
        return result.StdOut;
    }
}
