using System.Net;
using PhoenixStick.Dns;

namespace PhoenixStick.Tests.Dns;

/// <summary>Liefert vorbereitete Antworten statt echter HTTP-Anfragen.</summary>
internal class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request.RequestUri!.PathAndQuery);
        return Task.FromResult(respond(request));
    }
}

public class DnsTests
{
    private const string Stick = "192.168.178.26";

    private static string Entry(string time, string client, string domain, string reason = "NotFilteredNotFound") =>
        $$"""{ "time": "{{time}}", "client": "{{client}}", "question": { "name": "{{domain}}", "type": "A" }, "reason": "{{reason}}" }""";

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private static AdGuardClient Client(FakeHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://adguard.test/") });

    private static readonly DnsCatalog _catalog = new(
    [
        new("amazon-adsystem.com", DnsCategory.Ads, "Werbung", ""),
        new("device-metrics-us.amazon.com", DnsCategory.Tracking, "", ""),
        new("atv-ext.amazon.com", DnsCategory.Keep, "", ""),
        new("keep.amazon-adsystem.com", DnsCategory.Keep, "", ""),
    ]);

    [Theory]
    [InlineData("aax-eu.amazon-adsystem.com", DnsCategory.Ads)]
    [InlineData("amazon-adsystem.com.", DnsCategory.Ads)]
    [InlineData("device-metrics-us.amazon.com", DnsCategory.Tracking)]
    [InlineData("atv-ext-eu.amazon.com", null)]
    [InlineData("notamazon-adsystem.com", null)]
    [InlineData("x.keep.amazon-adsystem.com", DnsCategory.Keep)]
    public void Classify_MatchesSubdomains_LongestWins(string domain, DnsCategory? expected) =>
        Assert.Equal(expected, _catalog.Classify(domain)?.Category);

    [Fact]
    public async Task QueryLog_FiltersClient_FollowsPagesUntilSince()
    {
        var since = new DateTime(2026, 10, 30, 10, 0, 0);
        var page = 0;
        var handler = new FakeHandler(_ => ++page == 1
            ? Json($$"""{ "data": [ {{string.Join(",", Enumerable.Range(0, 500).Select(i => Entry("2026-10-30T12:00:00", i == 0 ? "192.168.178.99" : Stick, "atv-ext.amazon.com.")))}} ], "oldest": "2026-10-30T12:00:00Z" }""")
            : Json($$"""{ "data": [ {{Entry("2026-10-30T11:00:00", Stick, "aax-eu.amazon-adsystem.com", "FilteredBlackList")}}, {{Entry("2026-10-30T09:00:00", Stick, "zu.alt.com")}} ], "oldest": "x" }"""));

        var queries = await Client(handler).ReadQueryLogAsync(Stick, since, 10000);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("older_than=", handler.Requests[1]);
        Assert.Equal(500, queries.Count);
        Assert.DoesNotContain(queries, q => q.Domain == "zu.alt.com");
        Assert.All(queries, q => Assert.Equal(Stick, q.Client));
        Assert.True(queries.Single(q => q.Domain == "aax-eu.amazon-adsystem.com").Blocked);
        Assert.Equal("atv-ext.amazon.com", queries[0].Domain);
    }

    [Fact]
    public async Task QueryLog_WrongPassword_GivesReadableError()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<AdGuardException>(() => Client(handler).ReadQueryLogAsync(Stick, DateTime.MinValue, 10));

        Assert.Contains("Passwort", ex.Message);
    }

    [Fact]
    public async Task QueryLog_OnlyReads()
    {
        var methods = new List<HttpMethod>();
        var handler = new FakeHandler(r => { methods.Add(r.Method); return Json("""{ "data": [] }"""); });

        await Client(handler).ReadQueryLogAsync(Stick, DateTime.MinValue, 10);

        Assert.All(methods, m => Assert.Equal(HttpMethod.Get, m));
    }

    [Fact]
    public void Report_GroupsAndSortsAdsFirst()
    {
        var t = new DateTime(2026, 10, 30, 12, 0, 0);
        DnsQuery[] queries =
        [
            new(t, Stick, "atv-ext.amazon.com", "A", false),
            new(t, Stick, "atv-ext.amazon.com", "A", false),
            new(t, Stick, "unbekannt.example.com", "A", false),
            new(t, Stick, "aax-eu.amazon-adsystem.com", "A", true),
        ];

        var stats = DnsReport.Build(queries, _catalog);

        Assert.Equal(["aax-eu.amazon-adsystem.com", "unbekannt.example.com", "atv-ext.amazon.com"], stats.Select(s => s.Domain));
        Assert.Equal(1, stats[0].Blocked);
        Assert.Equal(2, stats[2].Count);
    }

    [Fact]
    public void RealDnsCatalog_KeepsUpdatesAndPrimeVideo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PhoenixStick.sln")))
            dir = dir.Parent;
        var catalog = DnsCatalog.Load(Path.Combine(dir!.FullName, "catalog"));

        Assert.Equal(DnsCategory.Keep, catalog.Classify("softwareupdates.amazon.com")?.Category);
        Assert.Equal(DnsCategory.Keep, catalog.Classify("atv-ext.amazon.com")?.Category);
        Assert.Equal(DnsCategory.Keep, catalog.Classify("x.aiv-delivery.net")?.Category);
        Assert.Equal(DnsCategory.Keep, catalog.Classify("device-messaging-na.amazon.com")?.Category);
        Assert.Equal(DnsCategory.Ads, catalog.Classify("aax-eu.amazon-adsystem.com")?.Category);
    }
}
