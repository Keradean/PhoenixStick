using System.ComponentModel;
using PhoenixStick.Snapshot;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Pakete und Settings nach state/snapshots/<zeitstempel>/ sichern")]
public class SnapshotCommand : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken ct)
    {
        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var summary = await new SnapshotWriter(adb, StatePaths.Default()).WriteAsync(DateTime.Now, ct);

        AnsiConsole.MarkupLine($"[green]Snapshot gespeichert:[/] {Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), summary.Directory))}");
        AnsiConsole.MarkupLine($"  Pakete: {summary.PackageCount}");
        foreach (var (table, count) in summary.SettingsCounts)
            AnsiConsole.MarkupLine($"  Settings {table}: {count}");
        return 0;
    }
}
