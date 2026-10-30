using System.Text.Json;
using PhoenixStick.Catalog;
using PhoenixStick.Storage;

namespace PhoenixStick.Dns;

public enum DnsCategory
{
    Ads,
    Tracking,
    Keep,
    Unknown,
}

public record DnsCatalogEntry(string Domain, DnsCategory Category, string Note, string Source);

/// <summary>Bekannte Adressen aus catalog/dns.json. Ein Eintrag gilt auch für alle Subdomains.</summary>
public class DnsCatalog(IReadOnlyList<DnsCatalogEntry> entries)
{
    public const string FileName = "dns.json";

    public IReadOnlyList<DnsCatalogEntry> Entries { get; } = entries;

    /// <summary>Der längste passende Eintrag gewinnt, so kann "keep" eine breitere Sperre überstimmen.</summary>
    public DnsCatalogEntry? Classify(string domain)
    {
        domain = domain.TrimEnd('.').ToLowerInvariant();
        return Entries
            .Where(e => domain == e.Domain || domain.EndsWith("." + e.Domain, StringComparison.Ordinal))
            .MaxBy(e => e.Domain.Length);
    }

    private record RawEntry(string? Domain, DnsCategory? Category, string? Note, string? Source);

    public static DnsCatalog Load(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
            throw new CatalogException($"Katalogdatei fehlt: {path}");

        List<RawEntry> raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<RawEntry>>(File.ReadAllText(path), JsonDefaults.Options)
                ?? throw new CatalogException($"Katalogdatei enthält keine Liste: {path}");
        }
        catch (JsonException ex)
        {
            throw new CatalogException($"Katalogdatei ist kein gültiges JSON: {path}\n{ex.Message}", ex);
        }

        var entries = new List<DnsCatalogEntry>();
        for (var i = 0; i < raw.Count; i++)
        {
            var r = raw[i];
            if (string.IsNullOrWhiteSpace(r.Domain) || r.Category is null or DnsCategory.Unknown)
                throw new CatalogException($"{FileName}, Eintrag {i + 1}: \"domain\" oder \"category\" (ads, tracking, keep) fehlt");
            entries.Add(new DnsCatalogEntry(r.Domain.ToLowerInvariant(), r.Category.Value, r.Note ?? "", r.Source ?? ""));
        }
        return new DnsCatalog(entries);
    }
}
