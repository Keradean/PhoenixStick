using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Changes;
using PhoenixStick.Debloat;
using PhoenixStick.Inventory;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class DebloatSettings : ApplySettings
{
    [CommandOption("--preset <PRESET>")]
    [Description("safe, optional oder risky")]
    public string PresetName { get; init; } = "safe";

    [CommandOption("--include <GRUPPE>")]
    [Description("Gruppe zusätzlich mitnehmen: alexa, mirroring oder preview. Mehrfach angebbar")]
    public string[] Include { get; init; } = [];

    public Preset Preset => Enum.Parse<Preset>(PresetName, ignoreCase: true);

    // Enum.TryParse nimmt auch Zahlen wie "2" an, die sollen hier nicht gelten
    public override ValidationResult Validate() =>
        !int.TryParse(PresetName, out _) && Enum.TryParse<Preset>(PresetName, ignoreCase: true, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unbekanntes Preset \"{PresetName}\". Erlaubt sind safe, optional und risky.");
}

[Description("Pakete aus dem Katalog deaktivieren. Ohne --apply nur Probelauf")]
public class DebloatCommand : AsyncCommand<DebloatSettings>
{
    // Diese Gründe sind bei über 100 Paketen nur Rauschen, sie erscheinen als Zahl
    private static readonly SkipReason[] _countOnly = [SkipReason.NotInCatalog, SkipReason.NotInPreset, SkipReason.ThirdParty];

    public override async Task<int> ExecuteAsync(CommandContext context, DebloatSettings settings, CancellationToken ct)
    {
        var catalog = CatalogLoader.Load(CatalogLoader.DefaultDirectory());
        foreach (var warning in catalog.Warnings)
            AnsiConsole.MarkupLine($"[yellow]Warnung:[/] {Markup.Escape(warning)}");

        var adb = await DeviceSession.ConnectAsync(settings, ct, applyEnabled: settings.Apply);
        if (adb is null)
            return 1;

        var installed = await new InventoryReader(adb).ReadAsync(ct);
        var plan = DebloatPlanner.Plan(catalog, installed, settings.Preset, settings.Include);

        if (settings.Package is not null)
        {
            plan = FilterToPackage(plan, settings.Package);
            if (plan is null)
                return 1;
        }

        PrintPlan(plan, settings);

        if (!settings.Apply)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Das war ein Probelauf, am Gerät hat sich nichts geändert. Ausführen mit [bold]--apply[/].");
            return 0;
        }

        if (plan.Actions.Count == 0)
        {
            AnsiConsole.MarkupLine("Nichts zu tun.");
            return 0;
        }

        AnsiConsole.WriteLine();
        var paths = StatePaths.Default();
        await ChangeOutput.SnapshotAsync(adb, paths, ct);

        if (!AnsiConsole.Confirm($"[bold]{plan.Actions.Count}[/] Pakete jetzt deaktivieren?", defaultValue: false))
            return Cancelled();
        if (plan.Actions.Any(a => a.Entry.Category == Category.Risky)
            && !AnsiConsole.Confirm("[red]Darunter sind risky-Pakete mit unklarer Wirkung.[/] Wirklich fortfahren?", defaultValue: false))
            return Cancelled();

        var log = ChangeLog.Load(paths.ChangesFile);
        var summary = await new PackageChanger(adb, log).RunAsync(
            ChangeAction.Disable,
            plan.Actions.Select(a => a.Entry.Package).ToList(),
            ChangeOutput.PrintEntry,
            ct);

        ChangeOutput.PrintSummary(
            summary,
            "deaktiviert",
            skippedBeforeRun: plan.Skipped.Count(s => s.Reason == SkipReason.AlreadyDisabled),
            protectedBeforeRun: plan.Skipped.Count(s => s.Reason == SkipReason.Protected));
        return summary.Stopped ? 1 : 0;
    }

    private static int Cancelled()
    {
        AnsiConsole.MarkupLine("Abgebrochen, nichts geändert.");
        return 1;
    }

    private static DebloatPlan? FilterToPackage(DebloatPlan plan, string package)
    {
        var action = plan.Actions.Where(a => a.Entry.Package == package).ToList();
        if (action.Count > 0)
            return new DebloatPlan(action, []);

        var skip = plan.Skipped.FirstOrDefault(s => s.Package == package);
        var why = skip is null ? "nicht auf dem Gerät und nicht im Katalog" : DebloatPlanner.Describe(skip.Reason);
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(package)} wird nicht angefasst:[/] {Markup.Escape(why)}");
        if (skip?.Reason == SkipReason.NotInPreset)
            AnsiConsole.MarkupLine("Das Paket gehört zu einem anderen Preset, siehe --preset.");
        return null;
    }

    private static void PrintPlan(DebloatPlan plan, DebloatSettings settings)
    {
        var presetName = settings.Preset.ToString().ToLowerInvariant();
        var mode = settings.Apply ? "[red]Ausführen[/]" : "[bold]Probelauf[/]";
        AnsiConsole.MarkupLine($"{mode} mit Preset [bold]{presetName}[/].");
        if (settings.Include.Length > 0)
            AnsiConsole.MarkupLine($"Zusätzliche Gruppen: {Markup.Escape(string.Join(", ", settings.Include))}");
        if (settings.Preset == Preset.Risky)
            AnsiConsole.MarkupLine("[red]Achtung:[/] risky enthält Pakete mit unklarer Wirkung.");
        AnsiConsole.WriteLine();

        var actions = new Table()
            .Title($"Wird deaktiviert ({plan.Actions.Count})")
            .AddColumn("Paket")
            .AddColumn("Kategorie")
            .AddColumn("Grund")
            .AddColumn("Was kaputt gehen kann")
            .AddColumn("adb-Befehl");
        foreach (var action in plan.Actions)
        {
            var entry = action.Entry;
            actions.AddRow(
                Markup.Escape(entry.Package),
                CategoryMarkup(entry.Category),
                Markup.Escape(entry.Reason),
                Markup.Escape(entry.Breaks.Length > 0 ? entry.Breaks : "-"),
                $"[grey]{Markup.Escape(action.Command)}[/]");
        }
        AnsiConsole.Write(actions);

        var listed = plan.Skipped.Where(s => !_countOnly.Contains(s.Reason)).ToList();
        if (listed.Count > 0)
        {
            var skipped = new Table()
                .Title($"Übersprungen ({listed.Count})")
                .AddColumn("Paket")
                .AddColumn("Warum");
            foreach (var skip in listed.OrderBy(s => s.Reason).ThenBy(s => s.Package, StringComparer.Ordinal))
                skipped.AddRow(Markup.Escape(skip.Package), Markup.Escape(DebloatPlanner.Describe(skip.Reason)));
            AnsiConsole.Write(skipped);
        }

        foreach (var reason in _countOnly)
        {
            var count = plan.Skipped.Count(s => s.Reason == reason);
            if (count > 0)
                AnsiConsole.MarkupLine($"Außerdem übersprungen, {Markup.Escape(DebloatPlanner.Describe(reason))}: {count}");
        }
    }

    public static string CategoryMarkup(Category category) => category switch
    {
        Category.Safe => "[green]safe[/]",
        Category.Optional => "[yellow]optional[/]",
        Category.Risky => "[red]risky[/]",
        _ => category.ToString(),
    };
}
