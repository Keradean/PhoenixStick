using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Changes;
using PhoenixStick.Debloat;
using PhoenixStick.Inventory;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Drittanbieter-Apps aus restrictBackground in config.json den Autostart und das Laufen im Hintergrund verbieten. Ohne --apply nur Probelauf")]
public class BackgroundCommand : AsyncCommand<ApplySettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ApplySettings settings, CancellationToken ct)
    {
        var config = DeviceSession.LoadConfig(settings);
        var wanted = settings.Package is not null ? [settings.Package] : config.BackgroundRestricted;
        if (wanted.Count == 0)
        {
            AnsiConsole.MarkupLine("Keine Apps angegeben. In config.json unter \"restrictBackground\" eintragen oder --package nutzen.");
            return 0;
        }

        var catalog = CatalogLoader.Load(CatalogLoader.DefaultDirectory());
        var adb = await DeviceSession.ConnectAsync(settings, ct, applyEnabled: settings.Apply);
        if (adb is null)
            return 1;

        var installed = await new InventoryReader(adb).ReadAsync(ct);
        var restricted = await DeviceStateReader.ReadBackgroundRestrictedAsync(adb, ct);
        var plan = BackgroundPlanner.Plan(wanted, installed, catalog, restricted);

        var table = new Table().Title($"Hintergrund wird gesperrt ({plan.ToRestrict.Count})").AddColumn("App").AddColumn("adb-Befehl");
        foreach (var package in plan.ToRestrict)
            table.AddRow(Markup.Escape(package), $"[grey]{Markup.Escape(PackageCommands.RestrictBackground(package))}[/]");
        AnsiConsole.Write(table);
        foreach (var (package, reason) in plan.Skipped)
            AnsiConsole.MarkupLine($"Übersprungen, {Markup.Escape(reason)}: {Markup.Escape(package)}");

        AnsiConsole.MarkupLine("[grey]Die Apps bleiben benutzbar, dürfen aber nicht mehr im Hintergrund laufen. Wie streng Fire OS das durchsetzt, zeigt erst eine Messung mit perf.[/]");

        if (!settings.Apply)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Das war ein Probelauf, am Gerät hat sich nichts geändert. Ausführen mit [bold]--apply[/].");
            return 0;
        }

        if (plan.ToRestrict.Count == 0)
        {
            AnsiConsole.MarkupLine("Nichts zu tun.");
            return 0;
        }

        var paths = StatePaths.Default();
        await ChangeOutput.SnapshotAsync(adb, paths, ct);

        if (!AnsiConsole.Confirm($"[bold]{plan.ToRestrict.Count}[/] Apps im Hintergrund sperren?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("Abgebrochen, nichts geändert.");
            return 1;
        }

        var log = ChangeLog.Load(paths.ChangesFile);
        var summary = await new PackageChanger(adb, log).RunAsync(ChangeAction.RestrictBackground, plan.ToRestrict, ChangeOutput.PrintEntry, ct);
        ChangeOutput.PrintSummary(summary, "gesperrt", plan.Skipped.Count);
        return summary.Stopped ? 1 : 0;
    }
}

public record BackgroundPlan(IReadOnlyList<string> ToRestrict, IReadOnlyList<(string Package, string Reason)> Skipped);

/// <summary>Nur installierte Drittanbieter-Apps dürfen gesperrt werden, nie System- oder Never-touch-Pakete.</summary>
public static class BackgroundPlanner
{
    public static BackgroundPlan Plan(
        IReadOnlyList<string> wanted,
        IReadOnlyList<PackageInfo> installed,
        PackageCatalog catalog,
        IReadOnlySet<string> alreadyRestricted)
    {
        var byName = installed.ToDictionary(p => p.Name, StringComparer.Ordinal);
        var toRestrict = new List<string>();
        var skipped = new List<(string, string)>();

        foreach (var package in wanted.Distinct(StringComparer.Ordinal))
        {
            if (catalog.IsNeverTouch(package))
                skipped.Add((package, "never touch"));
            else if (!byName.TryGetValue(package, out var info))
                skipped.Add((package, "nicht installiert"));
            else if (info.IsSystem)
                skipped.Add((package, "Systempaket, nur Drittanbieter-Apps erlaubt"));
            else if (alreadyRestricted.Contains(package))
                skipped.Add((package, "schon gesperrt"));
            else
                toRestrict.Add(package);
        }
        return new BackgroundPlan(toRestrict, skipped);
    }
}
