using System.ComponentModel;
using System.Text.Json;
using PhoenixStick.Catalog;
using PhoenixStick.Dns;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class DnsSettings : GlobalSettings
{
    [CommandOption("--hours <STUNDEN>")]
    [Description("Zeitraum in Stunden, Standard 48")]
    public int Hours { get; init; } = 48;

    [CommandOption("--max <ANZAHL>")]
    [Description("Höchstens so viele Anfragen lesen, Standard 20000")]
    public int MaxEntries { get; init; } = 20000;
}

[Description("DNS-Anfragen des Sticks aus AdGuard Home auswerten und bekannte Werbe- und Tracking-Adressen markieren. Ändert nichts")]
public class DnsCommand : AsyncCommand<DnsSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DnsSettings settings, CancellationToken ct)
    {
        var config = DeviceSession.LoadConfig(settings);
        if (config.AdGuard is null)
        {
            AnsiConsole.MarkupLine("[red]In config.json fehlt \"adGuard\".[/] Anleitung: docs/DNS.md, Abschnitt 3.");
            return 1;
        }

        var catalog = DnsCatalog.Load(CatalogLoader.DefaultDirectory());
        var since = DateTime.Now.AddHours(-settings.Hours);
        var queries = await AdGuardClient.Create(config.AdGuard).ReadQueryLogAsync(config.Host, since, settings.MaxEntries, ct);

        if (queries.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Keine Anfragen von {Markup.Escape(config.Host)} in den letzten {settings.Hours} Stunden.[/]");
            AnsiConsole.MarkupLine("Nutzt der Stick schon AdGuard als DNS? Siehe docs/DNS.md, Abschnitt 2.");
            return 1;
        }

        var stats = DnsReport.Build(queries, catalog);

        var table = new Table()
            .Title($"DNS-Anfragen von {Markup.Escape(config.Host)}, letzte {settings.Hours} Stunden")
            .AddColumn("Adresse").AddColumn("Anfragen").AddColumn("Zuletzt").AddColumn("Kategorie").AddColumn("Hinweis");
        foreach (var stat in stats)
        {
            var category = CategoryMarkup(stat.Category);
            if (stat.Blocked > 0)
                category += $" [grey](blockiert: {stat.Blocked})[/]";
            table.AddRow(
                Markup.Escape(stat.Domain),
                stat.Count.ToString(),
                stat.LastSeen.ToString("dd.MM. HH:mm"),
                category,
                Markup.Escape(stat.Note));
        }
        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine(
            $"Anfragen: [bold]{queries.Count}[/], Adressen: [bold]{stats.Count}[/] "
            + $"(Werbung {Count(stats, DnsCategory.Ads)}, Tracking {Count(stats, DnsCategory.Tracking)}, "
            + $"unbekannt {Count(stats, DnsCategory.Unknown)}, muss bleiben {Count(stats, DnsCategory.Keep)})");

        var file = Export(stats, config.Host, since);
        AnsiConsole.MarkupLine($"Gespeichert: [grey]{Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), file))}[/]");
        AnsiConsole.MarkupLine("[grey]Am Gerät und in AdGuard wurde nichts geändert. Gesperrt wird später einzeln, mit Test am TV.[/]");
        return 0;
    }

    private static int Count(IReadOnlyList<DomainStat> stats, DnsCategory category) => stats.Count(s => s.Category == category);

    private static string CategoryMarkup(DnsCategory category) => category switch
    {
        DnsCategory.Ads => "[red]Werbung[/]",
        DnsCategory.Tracking => "[yellow]Tracking[/]",
        DnsCategory.Keep => "[green]muss bleiben[/]",
        _ => "[grey]unbekannt[/]",
    };

    private static string Export(IReadOnlyList<DomainStat> stats, string client, DateTime since)
    {
        var dir = Path.Combine(StatePaths.Default().Root, "dns");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{StatePaths.Timestamp(DateTime.Now)}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(new { client, since, domains = stats }, JsonDefaults.Options));
        return file;
    }
}
