using PhoenixStick.Changes;
using PhoenixStick.Snapshot;
using PhoenixStick.Storage;
using Spectre.Console;

namespace PhoenixStick.Commands;

/// <summary>Gemeinsame Ausgabe für debloat und restore mit --apply.</summary>
public static class ChangeOutput
{
    public static void PrintEntry(ChangeEntry entry)
    {
        var verb = entry.Action switch
        {
            ChangeAction.Disable => "deaktiviert",
            ChangeAction.Enable => "aktiviert",
            ChangeAction.RestrictBackground => "im Hintergrund gesperrt",
            ChangeAction.AllowBackground => "darf wieder im Hintergrund laufen",
            _ => entry.Action.ToString(),
        };
        var line = entry.Result switch
        {
            ChangeResult.Success => $"[green]OK[/]       {Markup.Escape(entry.Package)} {verb}",
            ChangeResult.Protected => $"[yellow]GESCHÜTZT[/] {Markup.Escape(entry.Package)}, übersprungen",
            _ => $"[red]FEHLER[/]   {Markup.Escape(entry.Package)}: {Markup.Escape(entry.DeviceMessage)}",
        };
        AnsiConsole.MarkupLine(line);
    }

    /// <param name="skippedBeforeRun">Schon in der Planung übersprungen, z. B. weil schon deaktiviert.</param>
    /// <param name="protectedBeforeRun">Laut Katalog geschützt, deshalb gar nicht erst versucht.</param>
    public static void PrintSummary(ChangeSummary summary, string verb, int skippedBeforeRun, int protectedBeforeRun = 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[bold]Zusammenfassung:[/] {verb}: {summary.Changed}, "
            + $"übersprungen: {summary.Skipped + skippedBeforeRun}, "
            + $"geschützt: {summary.Protected + protectedBeforeRun}, Fehler: {summary.Failed}");

        if (summary.Protected > 0)
            AnsiConsole.MarkupLine("[yellow]Fire OS hat Pakete als geschützt abgelehnt. Sie sind im Protokoll vermerkt.[/]");
        if (summary.Stopped)
            AnsiConsole.MarkupLine("[red]Abgebrochen wegen eines unerwarteten Fehlers. Bisherige Änderungen stehen in state/changes.json.[/]");
    }

    public static async Task SnapshotAsync(PhoenixStick.Adb.IAdbClient adb, StatePaths paths, CancellationToken ct)
    {
        var summary = await new SnapshotWriter(adb, paths).WriteAsync(DateTime.Now, ct);
        AnsiConsole.MarkupLine($"Snapshot gespeichert: [grey]{Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), summary.Directory))}[/]");
    }
}
