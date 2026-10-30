using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhoenixStick.Config;

namespace PhoenixStick.Dns;

public record DnsQuery(DateTime Time, string Client, string Domain, string Type, bool Blocked);

public class AdGuardException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Liest das Abfrageprotokoll von AdGuard Home über dessen HTTP-Schnittstelle. Ändert nichts.</summary>
public class AdGuardClient(HttpClient http)
{
    private const int PageSize = 500;

    private readonly HttpClient _http = http;

    public static AdGuardClient Create(AdGuardConfig config)
    {
        var http = new HttpClient { BaseAddress = new Uri(config.Url.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(20) };
        var password = Environment.GetEnvironmentVariable("PHOENIXSTICK_ADGUARD_PASSWORD") ?? config.Password;
        if (config.User is not null && password is not null)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.User}:{password}"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
        return new AdGuardClient(http);
    }

    /// <summary>Alle Anfragen von <paramref name="clientIp"/> seit <paramref name="since"/>, höchstens <paramref name="maxEntries"/>.</summary>
    public async Task<IReadOnlyList<DnsQuery>> ReadQueryLogAsync(string clientIp, DateTime since, int maxEntries, CancellationToken ct = default)
    {
        var result = new List<DnsQuery>();
        string? olderThan = null;

        // Das Protokoll kommt seitenweise, neueste zuerst. "oldest" ist der Startpunkt der nächsten Seite
        while (result.Count < maxEntries)
        {
            var url = $"control/querylog?limit={PageSize}&search={Uri.EscapeDataString(clientIp)}";
            if (olderThan is not null)
                url += $"&older_than={Uri.EscapeDataString(olderThan)}";

            using var doc = JsonDocument.Parse(await GetAsync(url, ct));
            var data = doc.RootElement.TryGetProperty("data", out var d) ? d : default;
            if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
                break;

            var reachedSince = false;
            foreach (var item in data.EnumerateArray())
            {
                var query = Parse(item);
                if (query.Time < since)
                {
                    reachedSince = true;
                    break;
                }
                // "search" sucht auch in Domainnamen, deshalb hier nochmal nach Client filtern
                if (query.Client == clientIp)
                    result.Add(query);
            }

            olderThan = doc.RootElement.TryGetProperty("oldest", out var oldest) ? oldest.GetString() : null;
            if (reachedSince || string.IsNullOrEmpty(olderThan) || data.GetArrayLength() < PageSize)
                break;
        }

        return result.Take(maxEntries).ToList();
    }

    private async Task<string> GetAsync(string url, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AdGuardException($"AdGuard Home nicht erreichbar ({_http.BaseAddress}). Läuft der Container, stimmt die URL in config.json?", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AdGuardException($"AdGuard Home antwortet nicht ({_http.BaseAddress}).", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new AdGuardException("AdGuard Home lehnt die Anmeldung ab. Benutzer und Passwort in config.json prüfen.");
            if (!response.IsSuccessStatusCode)
                throw new AdGuardException($"AdGuard Home meldet {(int)response.StatusCode} für {url}");
            return await response.Content.ReadAsStringAsync(ct);
        }
    }

    private static DnsQuery Parse(JsonElement item)
    {
        var question = item.GetProperty("question");
        var reason = item.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
        return new DnsQuery(
            item.GetProperty("time").GetDateTime(),
            item.TryGetProperty("client", out var c) ? c.GetString() ?? "" : "",
            (question.GetProperty("name").GetString() ?? "").TrimEnd('.').ToLowerInvariant(),
            question.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
            // Alle Sperrgründe von AdGuard beginnen mit "Filtered", z. B. FilteredBlackList
            reason.StartsWith("Filtered", StringComparison.Ordinal));
    }
}
