using System.Text.Json;
using PhoenixStick.Storage;

namespace PhoenixStick.Inventory;

public record InventoryFile(DateTime CreatedAt, string Device, int Count, IReadOnlyList<PackageInfo> Packages);

public static class InventoryExporter
{
    /// <returns>Pfad der geschriebenen Datei.</returns>
    public static string Export(StatePaths paths, string device, IReadOnlyList<PackageInfo> packages, DateTime now)
    {
        Directory.CreateDirectory(paths.InventoryDir);
        var file = Path.Combine(paths.InventoryDir, $"{StatePaths.Timestamp(now)}.json");

        var content = new InventoryFile(now, device, packages.Count, packages);
        File.WriteAllText(file, JsonSerializer.Serialize(content, JsonDefaults.Options));
        return file;
    }
}
