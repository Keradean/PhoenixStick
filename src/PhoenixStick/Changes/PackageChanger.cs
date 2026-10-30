using PhoenixStick.Adb;
using PhoenixStick.Debloat;

namespace PhoenixStick.Changes;

public record ChangeSummary(int Changed, int Skipped, int Protected, int Failed, bool Stopped);

/// <summary>Führt Paketänderungen nacheinander aus, prüft jede und protokolliert sie sofort.</summary>
public class PackageChanger(IAdbClient adb, ChangeLog log, Func<DateTime>? clock = null)
{
    private const string ProtectedMessage = "protected package";

    private readonly IAdbClient _adb = adb;
    private readonly ChangeLog _log = log;
    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.Now);

    /// <param name="onResult">Wird nach jedem Paket aufgerufen, z. B. für die Ausgabe.</param>
    public async Task<ChangeSummary> RunAsync(
        ChangeAction action,
        IReadOnlyList<string> packages,
        Action<ChangeEntry>? onResult = null,
        CancellationToken ct = default)
    {
        int changed = 0, skipped = 0, isProtected = 0, failed = 0;

        foreach (var package in packages)
        {
            var before = await GetStateAsync(package, action, ct);
            var target = action is ChangeAction.Disable or ChangeAction.RestrictBackground
                ? PackageState.Disabled
                : PackageState.Enabled;
            if (before == target)
            {
                // Nichts zu tun, wird auch nicht protokolliert
                skipped++;
                continue;
            }

            var entry = await ChangeAsync(package, action, before, target, ct);
            _log.Append(entry);
            onResult?.Invoke(entry);

            switch (entry.Result)
            {
                case ChangeResult.Success:
                    changed++;
                    break;
                case ChangeResult.Protected:
                    isProtected++;
                    break;
                default:
                    failed++;
                    return new ChangeSummary(changed, skipped, isProtected, failed, Stopped: true);
            }
        }

        return new ChangeSummary(changed, skipped, isProtected, failed, Stopped: false);
    }

    private async Task<ChangeEntry> ChangeAsync(
        string package, ChangeAction action, PackageState before, PackageState target, CancellationToken ct)
    {
        var command = action switch
        {
            ChangeAction.Disable => PackageCommands.Disable(package),
            ChangeAction.Enable => PackageCommands.Enable(package),
            ChangeAction.RestrictBackground => PackageCommands.RestrictBackground(package),
            ChangeAction.AllowBackground => PackageCommands.AllowBackground(package),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        string message;
        try
        {
            var result = await _adb.ShellWriteAsync(command, ct);
            message = (result.StdOut + result.StdErr).Trim();

            if (message.Contains(ProtectedMessage, StringComparison.OrdinalIgnoreCase))
                return Entry(package, action, before, ChangeResult.Protected, message);

            if (!result.Success)
                return Entry(package, action, before, ChangeResult.Failed, message);

            var after = await GetStateAsync(package, action, ct);
            if (after != target)
                return Entry(package, action, before, ChangeResult.Failed, $"{message}\nZustand danach: {after}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Entry(package, action, before, ChangeResult.Failed, ex.Message);
        }

        return Entry(package, action, before, ChangeResult.Success, message);
    }

    private ChangeEntry Entry(string package, ChangeAction action, PackageState before, ChangeResult result, string message) =>
        new(_clock(), package, action, before, result, message);

    private async Task<PackageState> GetStateAsync(string package, ChangeAction action, CancellationToken ct)
    {
        if (action is ChangeAction.RestrictBackground or ChangeAction.AllowBackground)
        {
            var restricted = await DeviceStateReader.ReadBackgroundRestrictedAsync(_adb, ct);
            return restricted.Contains(package) ? PackageState.Disabled : PackageState.Enabled;
        }

        var disabled = await DeviceStateReader.ReadDisabledAsync(_adb, ct);
        return disabled.Contains(package) ? PackageState.Disabled : PackageState.Enabled;
    }
}
