using System.Text.Json;
using PhoenixStick.Storage;

namespace PhoenixStick.Changes;

public enum ChangeAction
{
    Disable,
    Enable,
    RestrictBackground,
    AllowBackground,
    PutSetting,
    RestoreSetting,
}

/// <summary>Bei der Hintergrundsperre steht Enabled für "darf im Hintergrund laufen", Disabled für "gesperrt".</summary>
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

/// <param name="Package">Paketname, bei Settings die Kennung "tabelle/schlüssel".</param>
/// <param name="OldValue">Nur bei Settings: Wert vor der Änderung.</param>
/// <param name="NewValue">Nur bei Settings: gesetzter Wert.</param>
public record ChangeEntry(
    DateTime Timestamp,
    string Package,
    ChangeAction Action,
    PackageState StateBefore,
    ChangeResult Result,
    string DeviceMessage,
    string? OldValue = null,
    string? NewValue = null);

/// <param name="Id">Kennung "tabelle/schlüssel".</param>
/// <param name="OriginalValue">Wert, den restore wiederherstellt.</param>
/// <param name="ToolValue">Wert, den das Tool gesetzt hat.</param>
public record SettingChange(string Id, string OriginalValue, string ToolValue);

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
    public IReadOnlyList<string> PackagesDisabledByTool() => ChangedByTool(ChangeAction.Disable, ChangeAction.Enable);

    /// <summary>Apps, die das Tool im Hintergrund gesperrt und seitdem nicht wieder freigegeben hat.</summary>
    public IReadOnlyList<string> PackagesRestrictedByTool() =>
        ChangedByTool(ChangeAction.RestrictBackground, ChangeAction.AllowBackground);

    /// <summary>Settings, die das Tool gesetzt und seitdem nicht zurückgesetzt hat.</summary>
    public IReadOnlyList<SettingChange> SettingsChangedByTool()
    {
        var result = new List<SettingChange>();
        var relevant = _entries.Where(e =>
            e.Result == ChangeResult.Success && e.Action is ChangeAction.PutSetting or ChangeAction.RestoreSetting);
        foreach (var group in relevant.GroupBy(e => e.Package))
        {
            var last = group.Last();
            if (last.Action == ChangeAction.PutSetting && last.OldValue is not null && last.NewValue is not null)
                result.Add(new SettingChange(group.Key, last.OldValue, last.NewValue));
        }
        return result.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
    }

    private IReadOnlyList<string> ChangedByTool(ChangeAction apply, ChangeAction undo)
    {
        var result = new List<string>();
        var relevant = _entries.Where(e => e.Result == ChangeResult.Success && (e.Action == apply || e.Action == undo));
        foreach (var group in relevant.GroupBy(e => e.Package))
        {
            var last = group.Last();
            if (last.Action == apply && last.StateBefore == PackageState.Enabled)
                result.Add(group.Key);
        }
        return result.Order(StringComparer.Ordinal).ToList();
    }
}
