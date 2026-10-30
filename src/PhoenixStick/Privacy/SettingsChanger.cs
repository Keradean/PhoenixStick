using PhoenixStick.Adb;
using PhoenixStick.Changes;

namespace PhoenixStick.Privacy;

public record SettingTarget(string Table, string Key, string Value)
{
    public string Id => $"{Table}/{Key}";

    public static SettingTarget FromId(string id, string value)
    {
        var separator = id.IndexOf('/');
        return new SettingTarget(id[..separator], id[(separator + 1)..], value);
    }
}

/// <summary>
/// Setzt Settings nacheinander: alten Wert lesen, setzen, prüfen, sofort protokollieren (Sicherheitsregel 7).
/// Schlüssel, die es auf dem Gerät nicht gibt, werden nie angelegt.
/// </summary>
public class SettingsChanger(IAdbClient adb, ChangeLog log, Func<DateTime>? clock = null)
{
    // So meldet settings get einen Schlüssel, den es nicht gibt
    public const string Missing = "null";

    private readonly IAdbClient _adb = adb;
    private readonly ChangeLog _log = log;
    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.Now);

    public async Task<ChangeSummary> RunAsync(
        ChangeAction action,
        IReadOnlyList<SettingTarget> targets,
        Action<ChangeEntry>? onResult = null,
        CancellationToken ct = default)
    {
        int changed = 0, skipped = 0, failed = 0;

        foreach (var target in targets)
        {
            var before = await GetAsync(_adb, target.Table, target.Key, ct);
            if (before == Missing || before == target.Value)
            {
                skipped++;
                continue;
            }

            var entry = await PutAsync(target, action, before, ct);
            _log.Append(entry);
            onResult?.Invoke(entry);

            if (entry.Result != ChangeResult.Success)
            {
                failed++;
                return new ChangeSummary(changed, skipped, 0, failed, Stopped: true);
            }
            changed++;
        }
        return new ChangeSummary(changed, skipped, 0, failed, Stopped: false);
    }

    public static async Task<string> GetAsync(IAdbClient adb, string table, string key, CancellationToken ct)
    {
        var command = $"settings get {table} {key}";
        var result = await adb.ShellReadAsync(command, ct);
        if (!result.Success)
            throw new AdbCommandException(command, result);
        return result.StdOut.Trim();
    }

    private async Task<ChangeEntry> PutAsync(SettingTarget target, ChangeAction action, string before, CancellationToken ct)
    {
        string message;
        try
        {
            var result = await _adb.ShellWriteAsync($"settings put {target.Table} {target.Key} {target.Value}", ct);
            message = (result.StdOut + result.StdErr).Trim();
            if (!result.Success)
                return Entry(target, action, before, ChangeResult.Failed, message);

            var after = await GetAsync(_adb, target.Table, target.Key, ct);
            if (after != target.Value)
                return Entry(target, action, before, ChangeResult.Failed, $"{message}\nWert danach: {after}".Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Entry(target, action, before, ChangeResult.Failed, ex.Message);
        }

        return Entry(target, action, before, ChangeResult.Success, message);
    }

    // StateBefore ist bei Settings ohne Bedeutung, entscheidend sind OldValue und NewValue
    private ChangeEntry Entry(SettingTarget target, ChangeAction action, string before, ChangeResult result, string message) =>
        new(_clock(), target.Id, action, PackageState.Enabled, result, message, OldValue: before, NewValue: target.Value);
}
