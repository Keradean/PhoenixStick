using PhoenixStick.Adb;
using PhoenixStick.Config;
using PhoenixStick.Device;
using Spectre.Console;

namespace PhoenixStick.Commands;

/// <summary>Gemeinsamer Einstieg für alle Befehle: Config laden, verbinden, Status prüfen.</summary>
public static class DeviceSession
{
    /// <returns>Den Client, wenn das Gerät bereit ist, sonst null (Fehler ist dann schon ausgegeben).</returns>
    /// <param name="applyEnabled">Nur mit true lässt der Client schreibende Befehle durch (--apply).</param>
    public static async Task<IAdbClient?> ConnectAsync(GlobalSettings settings, CancellationToken ct, bool applyEnabled = false)
    {
        var config = LoadConfig(settings);
        var adb = new AdbClient(config.Serial, applyEnabled);

        var connection = await new DeviceConnector(adb).ConnectAsync(ct);
        if (connection.State == DeviceState.Device)
            return adb;

        AnsiConsole.MarkupLine($"[red]{Markup.Escape(config.Serial)}: {Markup.Escape(DeviceConnector.Explain(connection.State))}[/]");
        if (connection.ConnectMessage.Length > 0)
            AnsiConsole.MarkupLine($"[grey]adb connect: {Markup.Escape(connection.ConnectMessage)}[/]");
        return null;
    }

    public static AppConfig LoadConfig(GlobalSettings settings) =>
        ConfigLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ConfigLoader.FileName), settings.Host);
}
