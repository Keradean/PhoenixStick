namespace PhoenixStick.Device;

/// <summary>Wählt aus getprop die Werte aus, die das Tool anzeigt. Seriennummer und MAC bleiben bewusst draußen.</summary>
public static class DeviceInfo
{
    public static readonly (string Label, string Key)[] General =
    [
        ("Modellcode", "ro.product.model"),
        ("Hersteller", "ro.product.manufacturer"),
        ("Gerätecodename", "ro.product.device"),
        ("Android-Version", "ro.build.version.release"),
        ("SDK-Level", "ro.build.version.sdk"),
        ("Sicherheitspatch", "ro.build.version.security_patch"),
        ("Build-Datum", "ro.build.date"),
    ];

    private static readonly string[] _fireOsKeys =
    [
        "ro.build.version.name",
        "ro.build.display.id",
        "ro.build.version.number",
    ];

    /// <summary>Alle Schlüssel mit Bezug zu Fire OS, alphabetisch sortiert.</summary>
    public static IEnumerable<KeyValuePair<string, string>> FireOs(IReadOnlyDictionary<string, string> props) =>
        props
            .Where(p => p.Key.Contains("fireos", StringComparison.OrdinalIgnoreCase)
                || p.Key.StartsWith("ro.build.lab126.", StringComparison.Ordinal)
                || _fireOsKeys.Contains(p.Key))
            .OrderBy(p => p.Key, StringComparer.Ordinal);
}
