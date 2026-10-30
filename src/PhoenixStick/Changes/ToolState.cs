namespace PhoenixStick.Changes;

/// <param name="StillApplied">Änderung des Tools ist noch wirksam.</param>
/// <param name="Reverted">Änderung ist nicht mehr wirksam, z. B. nach einem Firmware-Update.</param>
/// <param name="Removed">Paket ist nicht mehr installiert, z. B. von Hand deinstalliert. Nichts zu tun.</param>
public record ToolStateReport(IReadOnlyList<string> StillApplied, IReadOnlyList<string> Reverted, IReadOnlyList<string> Removed);

/// <summary>Vergleicht das Protokoll mit dem aktuellen Zustand des Geräts. Grundlage für restore und check.</summary>
public static class ToolState
{
    /// <param name="installed">Installierte Pakete. Null heißt: alle gelten als installiert.</param>
    public static ToolStateReport ComparePackages(ChangeLog log, IReadOnlySet<string> disabledOnDevice, IReadOnlySet<string>? installed = null) =>
        Compare(log.PackagesDisabledByTool(), disabledOnDevice, installed);

    /// <param name="installed">Installierte Pakete. Null heißt: alle gelten als installiert.</param>
    public static ToolStateReport CompareBackground(ChangeLog log, IReadOnlySet<string> restrictedOnDevice, IReadOnlySet<string>? installed = null) =>
        Compare(log.PackagesRestrictedByTool(), restrictedOnDevice, installed);

    private static ToolStateReport Compare(IReadOnlyList<string> byTool, IReadOnlySet<string> appliedOnDevice, IReadOnlySet<string>? installed)
    {
        var present = byTool.Where(p => installed is null || installed.Contains(p)).ToList();
        return new ToolStateReport(
            present.Where(appliedOnDevice.Contains).ToList(),
            present.Where(p => !appliedOnDevice.Contains(p)).ToList(),
            byTool.Except(present).ToList());
    }

    /// <param name="currentValues">Aktuelle Werte vom Gerät, Schlüssel ist die Kennung "tabelle/schlüssel".</param>
    public static SettingsReport CompareSettings(ChangeLog log, IReadOnlyDictionary<string, string> currentValues)
    {
        var changes = log.SettingsChangedByTool();
        return new SettingsReport(
            changes.Where(c => currentValues.GetValueOrDefault(c.Id) == c.ToolValue).ToList(),
            changes.Where(c => currentValues.GetValueOrDefault(c.Id) != c.ToolValue).ToList());
    }
}

/// <param name="StillApplied">Wert steht noch so, wie das Tool ihn gesetzt hat.</param>
/// <param name="Reverted">Wert wurde verändert, z. B. von Fire OS beim Booten.</param>
public record SettingsReport(IReadOnlyList<SettingChange> StillApplied, IReadOnlyList<SettingChange> Reverted);
