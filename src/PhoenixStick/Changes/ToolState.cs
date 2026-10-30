namespace PhoenixStick.Changes;

public record ToolStateReport(IReadOnlyList<string> StillDisabled, IReadOnlyList<string> ActiveAgain);

/// <summary>Vergleicht das Protokoll mit dem aktuellen Zustand des Geräts. Grundlage für restore und check.</summary>
public static class ToolState
{
    public static ToolStateReport Compare(ChangeLog log, IReadOnlySet<string> disabledOnDevice)
    {
        var byTool = log.PackagesDisabledByTool();
        return new ToolStateReport(
            byTool.Where(disabledOnDevice.Contains).ToList(),
            byTool.Where(p => !disabledOnDevice.Contains(p)).ToList());
    }
}
