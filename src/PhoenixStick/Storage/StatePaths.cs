namespace PhoenixStick.Storage;

/// <summary>Ablage unter state/ im aktuellen Verzeichnis (Projektordner).</summary>
public class StatePaths(string root)
{
    public string Root { get; } = root;

    public string InventoryDir => Path.Combine(Root, "inventory");

    public string SnapshotsDir => Path.Combine(Root, "snapshots");

    public static StatePaths Default() => new(Path.Combine(Directory.GetCurrentDirectory(), "state"));

    public static string Timestamp(DateTime time) => time.ToString("yyyy-MM-dd_HH-mm-ss");
}
