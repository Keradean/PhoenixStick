using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Debloat;
using PhoenixStick.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class DebloatSettings : GlobalSettings
{
    [CommandOption("--preset <PRESET>")]
    [Description("safe, optional oder risky")]
    public string PresetName { get; init; } = "safe";

    [CommandOption("--include <GRUPPE>")]
    [Description("Gruppe zusätzlich mitnehmen, z. B. alexa oder mirroring. Mehrfach angebbar")]
    public string[] Include { get; init; } = [];

    public Preset Preset => Enum.Parse<Preset>(PresetName, ignoreCase: true);

    // Enum.TryParse nimmt auch Zahlen wie "2" an, die sollen hier nicht gelten
    public override ValidationResult Validate() =>
        !int.TryParse(PresetName, out _) && Enum.TryParse<Preset>(PresetName, ignoreCase: true, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unbekanntes Preset \"{PresetName}\". Erlaubt sind safe, optional und risky.");
}

[Description("Probelauf: zeigt, welche Pakete deaktiviert würden und warum")]
public class DebloatCommand : AsyncCommand<DebloatSettings>
{
    // Diese Gründe sind bei über 100 Paketen nur Rauschen, sie erscheinen als Zahl
    private static readonly SkipReason[] _countOnly = [SkipReason.NotInCatalog, SkipReason.NotInPreset, SkipReason.ThirdParty];

    public override async Task<int> ExecuteAsync(CommandContext context, DebloatSettings settings, CancellationToken ct)
    {
        var catalog = CatalogLoader.Load(CatalogLoader.DefaultDirectory());
        foreach (var warning in catalog.Warnings)
            AnsiConsole.MarkupLine($"[yellow]Warnung:[/] {Markup.Escape(warning)}");

        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var installed = await new InventoryReader(adb).ReadAsync(ct);
        var plan = DebloatPlanner.Plan(catalog, installed, settings.Preset, settings.Include);

        var presetName = settings.Preset.ToString().ToLowerInvariant();
        AnsiConsole.MarkupLine($"[bold]Probelauf[/] mit Preset [bold]{presetName}[/]. Am Gerät wird nichts geändert.");
        if (settings.Include.Length > 0)
            AnsiConsole.MarkupLine($"Zusätzliche Gruppen: {Markup.Escape(string.Join(", ", settings.Include))}");
        if (settings.Preset == Preset.Risky)
            AnsiConsole.MarkupLine("[red]Achtung:[/] risky enthält Pakete mit unklarer Wirkung.");
        AnsiConsole.WriteLine();

        var actions = new Table()
            .Title($"Würde deaktiviert ({plan.Actions.Count})")
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

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Ausführen kommt in Goal 03 mit [bold]--apply[/]. Dieser Lauf hat nichts geändert.");
        return 0;
    }

    public static string CategoryMarkup(Category category) => category switch
    {
        Category.Safe => "[green]safe[/]",
        Category.Optional => "[yellow]optional[/]",
        Category.Risky => "[red]risky[/]",
        _ => category.ToString(),
    };
}
