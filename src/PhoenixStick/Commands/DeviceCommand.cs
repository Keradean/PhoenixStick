using System.ComponentModel;
using PhoenixStick.Adb;
using PhoenixStick.Device;
using PhoenixStick.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

[Description("Verbindung prüfen und Gerätedaten anzeigen")]
public class DeviceCommand : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken ct)
    {
        var adb = await DeviceSession.ConnectAsync(settings, ct);
        if (adb is null)
            return 1;

        var result = await adb.ShellReadAsync("getprop", ct);
        if (!result.Success)
            throw new AdbCommandException("getprop", result);
        var props = GetpropParser.Parse(result.StdOut);

        AnsiConsole.MarkupLine($"[green]{Markup.Escape(adb.Serial)}: verbunden[/]");
        AnsiConsole.WriteLine();

        var general = new Table().Title("Gerät").AddColumn("Feld").AddColumn("Wert").AddColumn("Quelle");
        foreach (var (label, key) in DeviceInfo.General)
            general.AddRow(label, Markup.Escape(props.GetValueOrDefault(key, "-")), $"[grey]{key}[/]");
        AnsiConsole.Write(general);

        var fireOs = new Table().Title("Fire OS").AddColumn("Schlüssel").AddColumn("Wert");
        foreach (var (key, value) in DeviceInfo.FireOs(props))
            fireOs.AddRow(Markup.Escape(key), Markup.Escape(value));
        AnsiConsole.Write(fireOs);

        return 0;
    }
}
