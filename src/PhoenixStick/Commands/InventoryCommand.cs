using System.ComponentModel;
using PhoenixStick.Catalog;
using PhoenixStick.Inventory;
using PhoenixStick.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Alle Pakete mit Status auflisten und nach state/inventory/ exportieren")]
public class InventoryCommand : AsyncCommand<GlobalSettings>
{
    private static readonly string[] _groupOrder = [PackageInfo.AmazonGroup, PackageInfo.AndroidGroup, PackageInfo.OtherGroup];

    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken ct)
    {
        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var catalog = CatalogLoader.Load(CatalogLoader.DefaultDirectory());
        var packages = await new InventoryReader(adb).ReadAsync(ct);

        foreach (var group in _groupOrder)
        {
            var members = packages.Where(p => p.Group == group).ToList();
            var table = new Table()
                .Title($"{group} ({members.Count})")
                .AddColumn("Paket")
                .AddColumn("Status")
                .AddColumn("Typ")
                .AddColumn("Kategorie");

            foreach (var package in members)
            {
                table.AddRow(
                    Markup.Escape(package.Name),
                    package.IsEnabled ? "[green]aktiviert[/]" : "[yellow]deaktiviert[/]",
                    package.IsSystem ? "System" : "[blue]Drittanbieter[/]",
                    CategoryMarkup(catalog, package));
            }
            AnsiConsole.Write(table);
        }

        var file = InventoryExporter.Export(StatePaths.Default(), adb.Serial, packages, DateTime.Now);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"Gesamt: [bold]{packages.Count}[/] Pakete, "
            + $"{packages.Count(p => p.IsSystem)} System, "
            + $"{packages.Count(p => !p.IsSystem)} Drittanbieter, "
            + $"{packages.Count(p => !p.IsEnabled)} deaktiviert");
        AnsiConsole.MarkupLine($"Exportiert nach [grey]{Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), file))}[/]");
        return 0;
    }

    private static string CategoryMarkup(PackageCatalog catalog, PackageInfo package)
    {
        if (catalog.IsNeverTouch(package.Name) || !package.IsSystem)
            return "[blue]never touch[/]";

        var entry = catalog.Find(package.Name);
        if (entry is null)
            return "[grey]unbekannt[/]";

        var markup = DebloatCommand.CategoryMarkup(entry.Category);
        return entry.Group is null ? markup : $"{markup} [grey]({Markup.Escape(entry.Group)})[/]";
    }
}
