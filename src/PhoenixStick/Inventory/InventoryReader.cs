using PhoenixStick.Adb;
using PhoenixStick.Parsing;

namespace PhoenixStick.Inventory;

public class InventoryReader(IAdbClient adb)
{
    private readonly IAdbClient _adb = adb;

    public async Task<IReadOnlyList<PackageInfo>> ReadAsync(CancellationToken ct = default)
    {
        var all = await ListAsync("pm list packages", ct);
        var disabled = (await ListAsync("pm list packages -d", ct)).ToHashSet();
        var system = (await ListAsync("pm list packages -s", ct)).ToHashSet();

        return all
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(name => new PackageInfo(name, !disabled.Contains(name), system.Contains(name)))
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ListAsync(string command, CancellationToken ct)
    {
        var result = await _adb.ShellReadAsync(command, ct);
        if (!result.Success)
            throw new AdbCommandException(command, result);
        return PackageListParser.Parse(result.StdOut);
    }
}
