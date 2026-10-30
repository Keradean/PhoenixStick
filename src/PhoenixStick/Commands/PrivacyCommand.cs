using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Changes;
using PhoenixStick.Privacy;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Telemetrie- und Werbe-Settings aus catalog/privacy.json setzen. Ohne --apply nur Probelauf")]
public class PrivacyCommand : AsyncCommand<ApplySettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ApplySettings settings, CancellationToken ct)
    {
        var catalog = PrivacyCatalog.Load(CatalogLoader.DefaultDirectory());
        var adb = await DeviceSession.ConnectAsync(settings, ct, applyEnabled: settings.Apply);
        if (adb is null)
            return 1;

        var rows = new List<(PrivacyEntry Entry, string Current)>();
        foreach (var entry in catalog)
            rows.Add((entry, await SettingsChanger.GetAsync(adb, entry.Table, entry.Key, ct)));

        var plan = PrivacyPlanner.Plan(rows);

        var table = new Table().Title("Datenschutz-Settings")
            .AddColumn("Einstellung").AddColumn("Schlüssel").AddColumn("Jetzt").AddColumn("Ziel").AddColumn("Was passiert");
        foreach (var (entry, current) in rows)
        {
            table.AddRow(
                Markup.Escape(entry.Label),
                $"[grey]{Markup.Escape(entry.Id)}[/]",
                Markup.Escape(current),
                Markup.Escape(entry.Value),
                StatusMarkup(PrivacyPlanner.StatusOf(entry, current)));
        }
        AnsiConsole.Write(table);

        var openMenu = plan.MenuOpen;
        if (openMenu.Count > 0)
        {
            AnsiConsole.MarkupLine("[yellow]Noch im TV-Menü umzustellen[/] (Einstellungen > Einstellungen > Datenschutzeinstellungen):");
            foreach (var entry in openMenu)
                AnsiConsole.MarkupLine($"  {Markup.Escape(entry.Label)} → AUS");
        }

        if (!settings.Apply)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Das war ein Probelauf, am Gerät hat sich nichts geändert. Ausführen mit [bold]--apply[/].");
            return 0;
        }

        if (plan.ToSet.Count == 0)
        {
            AnsiConsole.MarkupLine("Nichts zu setzen.");
            return 0;
        }

        var paths = StatePaths.Default();
        await ChangeOutput.SnapshotAsync(adb, paths, ct);

        if (!AnsiConsole.Confirm($"[bold]{plan.ToSet.Count}[/] Settings jetzt setzen?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("Abgebrochen, nichts geändert.");
            return 1;
        }

        var log = ChangeLog.Load(paths.ChangesFile);
        var targets = plan.ToSet.Select(e => new SettingTarget(e.Table, e.Key, e.Value)).ToList();
        var summary = await new SettingsChanger(adb, log).RunAsync(ChangeAction.PutSetting, targets, ChangeOutput.PrintEntry, ct);
        ChangeOutput.PrintSummary(summary, "gesetzt", plan.Skipped);
        return summary.Stopped ? 1 : 0;
    }

    private static string StatusMarkup(PrivacyStatus status) => status switch
    {
        PrivacyStatus.WillSet => "[yellow]wird gesetzt[/]",
        PrivacyStatus.AlreadySet => "[green]passt schon[/]",
        PrivacyStatus.Missing => "[grey]gibt es hier nicht, wird nicht angelegt[/]",
        PrivacyStatus.MenuOpen => "[yellow]im TV-Menü ausschalten[/]",
        PrivacyStatus.MenuDone => "[green]im Menü aus[/]",
        _ => status.ToString(),
    };
}

public enum PrivacyStatus
{
    WillSet,
    AlreadySet,
    Missing,
    MenuOpen,
    MenuDone,
}

public record PrivacyPlan(IReadOnlyList<PrivacyEntry> ToSet, IReadOnlyList<PrivacyEntry> MenuOpen, int Skipped);

public static class PrivacyPlanner
{
    public static PrivacyStatus StatusOf(PrivacyEntry entry, string current)
    {
        if (current == SettingsChanger.Missing)
            return PrivacyStatus.Missing;
        if (entry.Mode == PrivacyMode.Verify)
            return current == entry.Value ? PrivacyStatus.MenuDone : PrivacyStatus.MenuOpen;
        return current == entry.Value ? PrivacyStatus.AlreadySet : PrivacyStatus.WillSet;
    }

    /// <summary>Nur "set"-Einträge, die es gibt und die abweichen, werden geschrieben. "verify" nie.</summary>
    public static PrivacyPlan Plan(IReadOnlyList<(PrivacyEntry Entry, string Current)> rows)
    {
        var statuses = rows.Select(r => (r.Entry, Status: StatusOf(r.Entry, r.Current))).ToList();
        return new PrivacyPlan(
            statuses.Where(s => s.Status == PrivacyStatus.WillSet).Select(s => s.Entry).ToList(),
            statuses.Where(s => s.Status == PrivacyStatus.MenuOpen).Select(s => s.Entry).ToList(),
            statuses.Count(s => s.Status is PrivacyStatus.AlreadySet or PrivacyStatus.Missing));
    }
}
