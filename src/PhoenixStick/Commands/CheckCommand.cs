using System.ComponentModel;
using PhoenixStick.Changes;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Prüft nach einem Update, ob vom Tool deaktivierte Pakete wieder aktiv sind")]
public class CheckCommand : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken ct)
    {
        var log = ChangeLog.Load(StatePaths.Default().ChangesFile);
        if (log.PackagesDisabledByTool().Count == 0)
        {
            AnsiConsole.MarkupLine("Laut Protokoll hat das Tool keine Pakete deaktiviert. Nichts zu prüfen.");
            return 0;
        }

        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var report = ToolState.Compare(log, await RestoreCommand.ReadDisabledAsync(adb, ct));

        AnsiConsole.MarkupLine($"Noch deaktiviert: [green]{report.StillDisabled.Count}[/]");
        if (report.ActiveAgain.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Alles wie protokolliert.[/] Kein Paket ist wieder aktiv.");
            return 0;
        }

        var table = new Table().Title($"[yellow]Wieder aktiv ({report.ActiveAgain.Count})[/]").AddColumn("Paket");
        foreach (var package in report.ActiveAgain)
            table.AddRow(Markup.Escape(package));
        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("Vermutlich durch ein Firmware-Update. Erneut deaktivieren mit:");
        AnsiConsole.MarkupLine("  [bold]phoenixstick debloat --preset safe[/] (Probelauf), danach mit [bold]--apply[/]");
        // Exit-Code 2, damit ein Skript den Unterschied zu "alles ok" erkennt
        return 2;
    }
}
