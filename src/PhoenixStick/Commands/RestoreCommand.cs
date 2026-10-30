using System.ComponentModel;
using PhoenixStick.Changes;
using PhoenixStick.Debloat;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Änderungen des Tools zurücknehmen: Pakete aktivieren, Hintergrundsperren aufheben. Ohne --apply nur Probelauf")]
public class RestoreCommand : AsyncCommand<ApplySettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ApplySettings settings, CancellationToken ct)
    {
        var paths = StatePaths.Default();
        var log = ChangeLog.Load(paths.ChangesFile);
        if (log.PackagesDisabledByTool().Count == 0 && log.PackagesRestrictedByTool().Count == 0)
        {
            AnsiConsole.MarkupLine("Laut Protokoll hat das Tool nichts geändert. Nichts zu tun.");
            return 0;
        }

        var adb = await DeviceSession.ConnectAsync(settings, ct, applyEnabled: settings.Apply);
        if (adb is null)
            return 1;

        var installed = await DeviceStateReader.ReadInstalledAsync(adb, ct);
        var packages = ToolState.ComparePackages(log, await DeviceStateReader.ReadDisabledAsync(adb, ct), installed);
        var background = ToolState.CompareBackground(log, await DeviceStateReader.ReadBackgroundRestrictedAsync(adb, ct), installed);
        foreach (var package in packages.Removed.Concat(background.Removed).Distinct())
            AnsiConsole.MarkupLine($"[grey]Nicht mehr installiert, wird ignoriert: {Markup.Escape(package)}[/]");

        var toEnable = Filter(packages.StillApplied, settings.Package);
        var toAllow = Filter(background.StillApplied, settings.Package);
        var alreadyUndone = Filter(packages.Reverted, settings.Package).Concat(Filter(background.Reverted, settings.Package)).ToList();

        if (settings.Package is not null && toEnable.Count + toAllow.Count + alreadyUndone.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(settings.Package)} hat das Tool nicht geändert.[/] restore fasst nur eigene Änderungen an.");
            return 1;
        }

        var table = new Table().Title($"Wird zurückgenommen ({toEnable.Count + toAllow.Count})").AddColumn("Paket").AddColumn("Was").AddColumn("adb-Befehl");
        foreach (var package in toEnable)
            table.AddRow(Markup.Escape(package), "wieder aktivieren", $"[grey]{Markup.Escape(PackageCommands.Enable(package))}[/]");
        foreach (var package in toAllow)
            table.AddRow(Markup.Escape(package), "Hintergrund wieder erlauben", $"[grey]{Markup.Escape(PackageCommands.AllowBackground(package))}[/]");
        AnsiConsole.Write(table);
        foreach (var package in alreadyUndone)
            AnsiConsole.MarkupLine($"Übersprungen, schon wieder im Ausgangszustand: {Markup.Escape(package)}");

        if (!settings.Apply)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Das war ein Probelauf, am Gerät hat sich nichts geändert. Ausführen mit [bold]--apply[/].");
            return 0;
        }

        var total = toEnable.Count + toAllow.Count;
        if (total == 0)
        {
            AnsiConsole.MarkupLine("Nichts zu tun.");
            return 0;
        }

        if (!AnsiConsole.Confirm($"[bold]{total}[/] Änderungen zurücknehmen?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("Abgebrochen, nichts geändert.");
            return 1;
        }

        var changer = new PackageChanger(adb, log);
        var summary = await changer.RunAsync(ChangeAction.Enable, toEnable, ChangeOutput.PrintEntry, ct);
        if (!summary.Stopped)
        {
            var second = await changer.RunAsync(ChangeAction.AllowBackground, toAllow, ChangeOutput.PrintEntry, ct);
            summary = new ChangeSummary(
                summary.Changed + second.Changed,
                summary.Skipped + second.Skipped,
                summary.Protected + second.Protected,
                summary.Failed + second.Failed,
                second.Stopped);
        }

        ChangeOutput.PrintSummary(summary, "zurückgenommen", alreadyUndone.Count);
        return summary.Stopped ? 1 : 0;
    }

    private static List<string> Filter(IReadOnlyList<string> packages, string? only) =>
        only is null ? packages.ToList() : packages.Where(p => p == only).ToList();
}
