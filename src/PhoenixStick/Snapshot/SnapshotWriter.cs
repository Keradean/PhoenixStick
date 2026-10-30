using System.Text.Json;
using PhoenixStick.Adb;
using PhoenixStick.Inventory;
using PhoenixStick.Parsing;
using PhoenixStick.Storage;

namespace PhoenixStick.Snapshot;

public record SnapshotSummary(string Directory, int PackageCount, IReadOnlyDictionary<string, int> SettingsCounts);

/// <summary>Sichert Paketliste und alle Settings-Tabellen in einen eigenen Ordner pro Zeitpunkt.</summary>
public class SnapshotWriter(IAdbClient adb, StatePaths paths)
{
    public static readonly string[] SettingsTables = ["global", "secure", "system"];

    private readonly IAdbClient _adb = adb;
    private readonly StatePaths _paths = paths;

    public async Task<SnapshotSummary> WriteAsync(DateTime now, CancellationToken ct = default)
    {
        // Erst alles lesen, dann schreiben. So entsteht bei einem Fehler kein halber Snapshot
        var packages = await new InventoryReader(_adb).ReadAsync(ct);

        var settings = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var table in SettingsTables)
        {
            var command = $"settings list {table}";
            var result = await _adb.ShellReadAsync(command, ct);
            if (!result.Success)
                throw new AdbCommandException(command, result);
            settings[table] = SettingsParser.Parse(result.StdOut);
        }

        var dir = Path.Combine(_paths.SnapshotsDir, StatePaths.Timestamp(now));
        Directory.CreateDirectory(dir);

        var inventory = new InventoryFile(now, _adb.Serial, packages.Count, packages);
        await File.WriteAllTextAsync(
            Path.Combine(dir, "packages.json"), JsonSerializer.Serialize(inventory, JsonDefaults.Options), ct);

        foreach (var (table, values) in settings)
        {
            var sorted = new SortedDictionary<string, string>(values.ToDictionary(), StringComparer.Ordinal);
            await File.WriteAllTextAsync(
                Path.Combine(dir, $"settings_{table}.json"), JsonSerializer.Serialize(sorted, JsonDefaults.Options), ct);
        }

        return new SnapshotSummary(dir, packages.Count, settings.ToDictionary(s => s.Key, s => s.Value.Count));
    }
}
