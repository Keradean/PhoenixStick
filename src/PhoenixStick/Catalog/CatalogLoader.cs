using System.Text.Json;
using PhoenixStick.Storage;

namespace PhoenixStick.Catalog;

public class CatalogException(string message, Exception? inner = null) : Exception(message, inner);

public static class CatalogLoader
{
    public const string CatalogFile = "fireos.json";
    public const string NeverTouchFile = "never-touch.json";

    // Rohformat aus der Datei. Alles nullable, damit fehlende Felder auffallen
    // und nicht still zu "" oder Category.Safe werden
    private record RawEntry(
        string? Package, string? Label, Category? Category, string? Group,
        string? Reason, string? Breaks, string? Source, bool? Protected);

    private record RawNeverTouch(string? Package, string? Reason, string? Source);

    public static string DefaultDirectory() => Path.Combine(Directory.GetCurrentDirectory(), "catalog");

    public static PackageCatalog Load(string directory)
    {
        var warnings = new List<string>();

        var rawNeverTouch = Read<RawNeverTouch>(Path.Combine(directory, NeverTouchFile));
        var neverTouch = new List<NeverTouchEntry>();
        var neverTouchNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rawNeverTouch.Count; i++)
        {
            var raw = rawNeverTouch[i];
            var package = Require(raw.Package, NeverTouchFile, i, "package");
            if (!neverTouchNames.Add(package))
            {
                warnings.Add($"{NeverTouchFile}: {package} steht doppelt drin");
                continue;
            }
            neverTouch.Add(new NeverTouchEntry(package, raw.Reason ?? "", raw.Source ?? ""));
        }

        var rawEntries = Read<RawEntry>(Path.Combine(directory, CatalogFile));
        var entries = new List<CatalogEntry>();
        var entryNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rawEntries.Count; i++)
        {
            var raw = rawEntries[i];
            var package = Require(raw.Package, CatalogFile, i, "package");
            var category = raw.Category
                ?? throw new CatalogException($"{CatalogFile}, Eintrag {i + 1} ({package}): \"category\" fehlt");

            if (!entryNames.Add(package))
            {
                warnings.Add($"{CatalogFile}: {package} steht doppelt drin, nur der erste Eintrag zählt");
                continue;
            }

            // Never-touch gewinnt immer, egal was im Katalog steht
            if (neverTouchNames.Contains(package))
            {
                warnings.Add($"{package} steht in {NeverTouchFile} und wird aus dem Katalog entfernt");
                continue;
            }

            entries.Add(new CatalogEntry(
                package,
                raw.Label ?? package,
                category,
                string.IsNullOrWhiteSpace(raw.Group) ? null : raw.Group,
                Require(raw.Reason, CatalogFile, i, "reason"),
                raw.Breaks ?? "",
                raw.Source ?? "",
                raw.Protected ?? false));
        }

        return new PackageCatalog(entries, neverTouch, warnings);
    }

    private static List<T> Read<T>(string path)
    {
        if (!File.Exists(path))
            throw new CatalogException($"Katalogdatei fehlt: {path}");

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            throw new CatalogException($"Katalogdatei ist leer: {path}");

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonDefaults.Options)
                ?? throw new CatalogException($"Katalogdatei enthält keine Liste: {path}");
        }
        catch (JsonException ex)
        {
            var position = ex.LineNumber is { } line ? $" (Zeile {line + 1})" : "";
            throw new CatalogException($"Katalogdatei ist kein gültiges JSON{position}: {path}\n{ex.Message}", ex);
        }
    }

    private static string Require(string? value, string file, int index, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new CatalogException($"{file}, Eintrag {index + 1}: \"{field}\" fehlt oder ist leer")
            : value;
}
