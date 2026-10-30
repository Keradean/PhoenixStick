using System.ComponentModel;
using PhoenixStick.Changes;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Prüft nach einem Update, ob Änderungen des Tools noch wirksam sind")]
public class CheckCommand : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken ct)
    {
        var log = ChangeLog.Load(StatePaths.Default().ChangesFile);
        if (log.PackagesDisabledByTool().Count == 0 && log.PackagesRestrictedByTool().Count == 0)
        {
            AnsiConsole.MarkupLine("Laut Protokoll hat das Tool nichts geändert. Nichts zu prüfen.");
            return 0;
        }

        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var installed = await DeviceStateReader.ReadInstalledAsync(adb, ct);
        var packages = ToolState.ComparePackages(log, await DeviceStateReader.ReadDisabledAsync(adb, ct), installed);
        var background = ToolState.CompareBackground(log, await DeviceStateReader.ReadBackgroundRestrictedAsync(adb, ct), installed);
        foreach (var package in packages.Removed.Concat(background.Removed).Distinct())
            AnsiConsole.MarkupLine($"[grey]Nicht mehr installiert, wird ignoriert: {Markup.Escape(package)}[/]");

        AnsiConsole.MarkupLine($"Noch deaktiviert: [green]{packages.StillApplied.Count}[/]");
        AnsiConsole.MarkupLine($"Noch im Hintergrund gesperrt: [green]{background.StillApplied.Count}[/]");

        if (packages.Reverted.Count == 0 && background.Reverted.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Alles wie protokolliert.[/] Keine Änderung wurde zurückgesetzt.");
            return 0;
        }

        var table = new Table().Title("[yellow]Zurückgesetzt[/]").AddColumn("Paket").AddColumn("Was");
        foreach (var package in packages.Reverted)
            table.AddRow(Markup.Escape(package), "wieder aktiv");
        foreach (var package in background.Reverted)
            table.AddRow(Markup.Escape(package), "darf wieder im Hintergrund laufen");
        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("Vermutlich durch ein Firmware-Update. Erneut anwenden, jeweils erst als Probelauf:");
        if (packages.Reverted.Count > 0)
            AnsiConsole.MarkupLine("  [bold]phoenixstick debloat --preset ...[/], danach mit [bold]--apply[/]");
        if (background.Reverted.Count > 0)
            AnsiConsole.MarkupLine("  [bold]phoenixstick background[/], danach mit [bold]--apply[/]");
        // Exit-Code 2, damit ein Skript den Unterschied zu "alles ok" erkennt
        return 2;
    }
}
