namespace PhoenixStick.Catalog;

public record CatalogEntry(
    string Package,
    string Label,
    Category Category,
    string? Group,
    string Reason,
    string Breaks,
    string Source,
    bool Protected);

public record NeverTouchEntry(string Package, string Reason, string Source);

/// <summary>Geladener Katalog. Never-touch-Pakete sind bereits aus <see cref="Entries"/> entfernt.</summary>
public class PackageCatalog(
    IReadOnlyList<CatalogEntry> entries,
    IReadOnlyList<NeverTouchEntry> neverTouch,
    IReadOnlyList<string> warnings)
{
    private readonly Dictionary<string, CatalogEntry> _byPackage = entries.ToDictionary(e => e.Package, StringComparer.Ordinal);
    private readonly HashSet<string> _neverTouch = neverTouch.Select(n => n.Package).ToHashSet(StringComparer.Ordinal);

    public IReadOnlyList<CatalogEntry> Entries { get; } = entries;

    public IReadOnlyList<NeverTouchEntry> NeverTouch { get; } = neverTouch;

    public IReadOnlyList<string> Warnings { get; } = warnings;

    public bool IsNeverTouch(string package) => _neverTouch.Contains(package);

    public CatalogEntry? Find(string package) => _byPackage.GetValueOrDefault(package);
}
