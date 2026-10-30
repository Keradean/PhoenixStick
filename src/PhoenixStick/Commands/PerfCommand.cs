using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Perf;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class PerfSettings : GlobalSettings
{
    [CommandOption("--note <TEXT>")]
    [Description("Notiz zur Messung, z. B. \"nach safe, Startbildschirm\"")]
    public string Note { get; init; } = "";
}

[Description("RAM und CPU messen, speichern und mit der letzten Messung vergleichen")]
public class PerfCommand : AsyncCommand<PerfSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PerfSettings settings, CancellationToken ct)
    {
        var catalog = CatalogLoader.Load(CatalogLoader.DefaultDirectory());
        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var recorder = new PerfRecorder(adb, StatePaths.Default());
        var previous = recorder.LoadLatest();
        var report = await recorder.MeasureAsync(settings.Note, DateTime.Now, ct);
        var file = recorder.Save(report);

        var uptime = TimeSpan.FromMilliseconds(report.UptimeMs);
        AnsiConsole.MarkupLine($"Seit Neustart: [bold]{(int)uptime.TotalHours}:{uptime.Minutes:00} h[/]");
        if (uptime < TimeSpan.FromMinutes(3))
            AnsiConsole.MarkupLine("[yellow]Der Stick startet noch. Für vergleichbare Werte 3 Minuten nach dem Neustart messen.[/]");

        var summary = new Table().Title("Übersicht").AddColumn("Wert").AddColumn("Jetzt").AddColumn("Letzte Messung").AddColumn("Differenz");
        AddRow(summary, "RAM frei", report.FreeRamKb, previous?.FreeRamKb, higherIsBetter: true);
        AddRow(summary, "RAM belegt", report.UsedRamKb, previous?.UsedRamKb, higherIsBetter: false);
        summary.AddRow(
            "Prozesse",
            report.ProcessCount.ToString(),
            previous?.ProcessCount.ToString() ?? "-",
            previous is null ? "-" : Delta(report.ProcessCount - previous.ProcessCount, "", higherIsBetter: false));
        summary.AddRow(
            "Last (1/5/15 min)",
            $"{report.Load1:0.00} / {report.Load5:0.00} / {report.Load15:0.00}",
            previous is null ? "-" : $"{previous.Load1:0.00} / {previous.Load5:0.00} / {previous.Load15:0.00}",
            "");
        AnsiConsole.Write(summary);
        if (previous is not null)
        {
            var note = previous.Note.Length > 0 ? $", Notiz: {previous.Note}" : "";
            AnsiConsole.MarkupLine($"[grey]Letzte Messung: {previous.Timestamp:dd.MM. HH:mm}{Markup.Escape(note)}[/]");
        }

        var memory = new Table().Title("Größte RAM-Verbraucher").AddColumn("Prozess").AddColumn("RAM (PSS)").AddColumn("Kategorie");
        foreach (var process in report.TopMemory)
            memory.AddRow(Markup.Escape(process.Name), $"{process.PssKb / 1024.0:0} MB", CategoryMarkup(catalog, process.Name));
        AnsiConsole.Write(memory);

        var cpu = new Table().Title("Größte CPU-Verbraucher (seit letzter Auswertung)").AddColumn("Prozess").AddColumn("CPU").AddColumn("Kategorie");
        foreach (var process in report.TopCpu)
            cpu.AddRow(Markup.Escape(process.Name), $"{process.Percent:0.#} %", CategoryMarkup(catalog, process.Name));
        AnsiConsole.Write(cpu);

        AnsiConsole.MarkupLine($"Gespeichert: [grey]{Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), file))}[/]");
        AnsiConsole.MarkupLine("[grey]Fair vergleichen: gleiche Bedingungen, z. B. 3 Minuten nach Neustart auf dem Startbildschirm, keine App offen.[/]");
        return 0;
    }

    private static void AddRow(Table table, string label, long nowKb, long? previousKb, bool higherIsBetter) =>
        table.AddRow(
            label,
            Mb(nowKb),
            previousKb is null ? "-" : Mb(previousKb.Value),
            previousKb is null ? "-" : Delta((nowKb - previousKb.Value) / 1024, " MB", higherIsBetter));

    private static string Mb(long kb) => $"{kb / 1024} MB";

    private static string Delta(long delta, string unit, bool higherIsBetter)
    {
        if (delta == 0)
            return $"0{unit}";
        var better = delta > 0 == higherIsBetter;
        return $"[{(better ? "green" : "red")}]{delta:+0;-0}{unit}[/]";
    }

    // Unterprozesse wie "com.amazon.x:remote" gehören zum Paket "com.amazon.x"
    private static string CategoryMarkup(PackageCatalog catalog, string processName)
    {
        var package = processName.Split(':')[0];
        if (catalog.IsNeverTouch(package))
            return "[blue]never touch[/]";
        var entry = catalog.Find(package);
        if (entry is null)
            return "";
        var markup = DebloatCommand.CategoryMarkup(entry.Category);
        if (entry.Protected)
            markup += " [grey](geschützt)[/]";
        return entry.Group is null ? markup : $"{markup} [grey]({Markup.Escape(entry.Group)})[/]";
    }
}
