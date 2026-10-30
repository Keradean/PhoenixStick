using PhoenixStick.Adb;
using PhoenixStick.Parsing;

namespace PhoenixStick.Changes;

/// <summary>Liest den aktuellen Zustand vom Gerät, den das Tool ändern kann.</summary>
public static class DeviceStateReader
{
    public static async Task<IReadOnlySet<string>> ReadInstalledAsync(IAdbClient adb, CancellationToken ct) =>
        PackageListParser.Parse(await ReadAsync(adb, "pm list packages", ct)).ToHashSet(StringComparer.Ordinal);

    public static async Task<IReadOnlySet<string>> ReadDisabledAsync(IAdbClient adb, CancellationToken ct) =>
        PackageListParser.Parse(await ReadAsync(adb, "pm list packages -d", ct)).ToHashSet(StringComparer.Ordinal);

    public static async Task<IReadOnlySet<string>> ReadBackgroundRestrictedAsync(IAdbClient adb, CancellationToken ct) =>
        AppOpsParser.BackgroundRestricted(await ReadAsync(adb, "dumpsys appops", ct));

    private static async Task<string> ReadAsync(IAdbClient adb, string command, CancellationToken ct)
    {
        var result = await adb.ShellReadAsync(command, ct);
        if (!result.Success)
            throw new AdbCommandException(command, result);
        return result.StdOut;
    }
}
