using System.ComponentModel;
using PhoenixStick.Adb;
using PhoenixStick.Changes;
using PhoenixStick.Parsing;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Vom Tool deaktivierte Pakete wieder aktivieren. Ohne --apply nur Probelauf")]
public class RestoreCommand : AsyncCommand<ApplySettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ApplySettings settings, CancellationToken ct)
    {
        var paths = StatePaths.Default();
        var log = ChangeLog.Load(paths.ChangesFile);
        if (log.PackagesDisabledByTool().Count == 0)
        {
            AnsiConsole.MarkupLine("Laut Protokoll hat das Tool keine Pakete deaktiviert. Nichts zu tun.");
            return 0;
        }

        var adb = await DeviceSession.ConnectAsync(settings, ct, applyEnabled: settings.Apply);
        if (adb is null)
            return 1;

        var report = ToolState.Compare(log, await ReadDisabledAsync(adb, ct));
        var toRestore = report.StillDisabled.ToList();
        var activeAgain = report.ActiveAgain.ToList();

        if (settings.Package is not null)
        {
            if (!toRestore.Contains(settings.Package) && !activeAgain.Contains(settings.Package))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(settings.Package)} hat das Tool nicht deaktiviert.[/] restore fasst nur eigene Änderungen an.");
                return 1;
            }
            toRestore = toRestore.Where(p => p == settings.Package).ToList();
            activeAgain = activeAgain.Where(p => p == settings.Package).ToList();
        }

        var table = new Table().Title($"Wird wieder aktiviert ({toRestore.Count})").AddColumn("Paket").AddColumn("adb-Befehl");
        foreach (var package in toRestore)
            table.AddRow(Markup.Escape(package), $"[grey]{Markup.Escape(Debloat.PackageCommands.Enable(package))}[/]");
        AnsiConsole.Write(table);
        foreach (var package in activeAgain)
            AnsiConsole.MarkupLine($"Übersprungen, schon wieder aktiv: {Markup.Escape(package)}");

        if (!settings.Apply)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Das war ein Probelauf, am Gerät hat sich nichts geändert. Ausführen mit [bold]--apply[/].");
            return 0;
        }

        if (toRestore.Count == 0)
        {
            AnsiConsole.MarkupLine("Nichts zu tun.");
            return 0;
        }

        if (!AnsiConsole.Confirm($"[bold]{toRestore.Count}[/] Pakete wieder aktivieren?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("Abgebrochen, nichts geändert.");
            return 1;
        }

        var summary = await new PackageChanger(adb, log).RunAsync(ChangeAction.Enable, toRestore, ChangeOutput.PrintEntry, ct);
        ChangeOutput.PrintSummary(summary, "aktiviert", activeAgain.Count);
        return summary.Stopped ? 1 : 0;
    }

    public static async Task<IReadOnlySet<string>> ReadDisabledAsync(IAdbClient adb, CancellationToken ct)
    {
        const string command = "pm list packages -d";
        var result = await adb.ShellReadAsync(command, ct);
        if (!result.Success)
            throw new AdbCommandException(command, result);
        return PackageListParser.Parse(result.StdOut).ToHashSet(StringComparer.Ordinal);
    }
}
