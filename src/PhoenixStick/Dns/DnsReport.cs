namespace PhoenixStick.Dns;

public record DomainStat(string Domain, int Count, int Blocked, DateTime LastSeen, DnsCategory Category, string Note);

public static class DnsReport
{
    // Reihenfolge der Ausgabe: zuerst, was weg kann, zum Schluss, was bleiben muss
    private static readonly DnsCategory[] _order = [DnsCategory.Ads, DnsCategory.Tracking, DnsCategory.Unknown, DnsCategory.Keep];

    public static IReadOnlyList<DomainStat> Build(IReadOnlyList<DnsQuery> queries, DnsCatalog catalog) =>
        queries
            .GroupBy(q => q.Domain)
            .Select(g =>
            {
                var entry = catalog.Classify(g.Key);
                return new DomainStat(
                    g.Key,
                    g.Count(),
                    g.Count(q => q.Blocked),
                    g.Max(q => q.Time),
                    entry?.Category ?? DnsCategory.Unknown,
                    entry?.Note ?? "");
            })
            .OrderBy(s => Array.IndexOf(_order, s.Category))
            .ThenByDescending(s => s.Count)
            .ThenBy(s => s.Domain, StringComparer.Ordinal)
            .ToList();
}
