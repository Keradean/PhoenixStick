using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixStick.Catalog;
using PhoenixStick.Storage;

namespace PhoenixStick.Privacy;

public enum PrivacyMode
{
    /// <summary>Das Tool setzt den Wert.</summary>
    Set,

    /// <summary>Dennis stellt es im TV-Menü um, das Tool prüft nur.</summary>
    Verify,
}

public record PrivacyEntry(string Table, string Key, string Value, string Label, string Reason, string Source, PrivacyMode Mode)
{
    /// <summary>Kennung im Änderungsprotokoll, z. B. "secure/limit_ad_tracking".</summary>
    public string Id => $"{Table}/{Key}";
}

public static partial class PrivacyCatalog
{
    public const string FileName = "privacy.json";

    private static readonly string[] _tables = ["global", "secure", "system"];

    // Schlüssel und Werte landen in einem Shell-Befehl, deshalb nur harmlose Zeichen
    [GeneratedRegex(@"^[A-Za-z0-9_.:\-]+$")]
    private static partial Regex SafeToken();

    private record RawEntry(string? Table, string? Key, string? Value, string? Label, string? Reason, string? Source, PrivacyMode? Mode);

    public static IReadOnlyList<PrivacyEntry> Load(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
            throw new CatalogException($"Katalogdatei fehlt: {path}");

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            throw new CatalogException($"Katalogdatei ist leer: {path}");

        List<RawEntry> raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<RawEntry>>(json, JsonDefaults.Options)
                ?? throw new CatalogException($"Katalogdatei enthält keine Liste: {path}");
        }
        catch (JsonException ex)
        {
            throw new CatalogException($"Katalogdatei ist kein gültiges JSON: {path}\n{ex.Message}", ex);
        }

        var entries = new List<PrivacyEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < raw.Count; i++)
        {
            var r = raw[i];
            var where = $"{FileName}, Eintrag {i + 1}";
            if (r.Table is null || !_tables.Contains(r.Table))
                throw new CatalogException($"{where}: \"table\" muss global, secure oder system sein");
            if (r.Key is null || !SafeToken().IsMatch(r.Key))
                throw new CatalogException($"{where}: \"key\" fehlt oder enthält unerlaubte Zeichen");
            if (r.Value is null || !SafeToken().IsMatch(r.Value))
                throw new CatalogException($"{where}: \"value\" fehlt oder enthält unerlaubte Zeichen");
            if (r.Mode is null)
                throw new CatalogException($"{where}: \"mode\" fehlt (set oder verify)");

            var entry = new PrivacyEntry(r.Table, r.Key, r.Value, r.Label ?? r.Key, r.Reason ?? "", r.Source ?? "", r.Mode.Value);
            if (!seen.Add(entry.Id))
                throw new CatalogException($"{where}: {entry.Id} steht doppelt drin");
            entries.Add(entry);
        }
        return entries;
    }
}
