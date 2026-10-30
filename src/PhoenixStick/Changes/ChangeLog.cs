using System.Text.Json;
using PhoenixStick.Storage;

namespace PhoenixStick.Changes;

public enum ChangeAction
{
    Disable,
    Enable,
}

public enum PackageState
{
    Enabled,
    Disabled,
}

public enum ChangeResult
{
    Success,
    Protected,
    Failed,
}

public record ChangeEntry(
    DateTime Timestamp,
    string Package,
    ChangeAction Action,
    PackageState StateBefore,
    ChangeResult Result,
    string DeviceMessage);

/// <summary>Änderungsprotokoll in state/changes.json. Jeder Eintrag wird sofort auf die Platte geschrieben.</summary>
public class ChangeLog
{
    private readonly string _path;
    private readonly List<ChangeEntry> _entries;

    private ChangeLog(string path, List<ChangeEntry> entries)
    {
        _path = path;
        _entries = entries;
    }

    public IReadOnlyList<ChangeEntry> Entries => _entries;

    public static ChangeLog Load(string path)
    {
        if (!File.Exists(path))
            return new ChangeLog(path, []);

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return new ChangeLog(path, []);

        try
        {
            var entries = JsonSerializer.Deserialize<List<ChangeEntry>>(json, JsonDefaults.Options) ?? [];
            return new ChangeLog(path, entries);
        }
        catch (JsonException ex)
        {
            // Nicht überschreiben: das Protokoll ist die Grundlage für restore
            throw new InvalidDataException($"Änderungsprotokoll ist beschädigt und wird nicht angefasst: {path}\n{ex.Message}", ex);
        }
    }

    public void Append(ChangeEntry entry)
    {
        _entries.Add(entry);

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        // Erst in eine temporäre Datei schreiben, dann ersetzen. So ist die Datei nie halb geschrieben
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_entries, JsonDefaults.Options));
        File.Move(temp, _path, overwrite: true);
    }

    /// <summary>
    /// Pakete, die das Tool deaktiviert hat und seitdem nicht wieder aktiviert hat.
    /// Pakete, die schon vorher aus waren, zählen nie dazu.
    /// </summary>
    public IReadOnlyList<string> PackagesDisabledByTool()
    {
        var result = new List<string>();
        foreach (var group in _entries.Where(e => e.Result == ChangeResult.Success).GroupBy(e => e.Package))
        {
            var last = group.Last();
            if (last.Action == ChangeAction.Disable && last.StateBefore == PackageState.Enabled)
                result.Add(group.Key);
        }
        return result.Order(StringComparer.Ordinal).ToList();
    }
}
