using PhoenixStick.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("phoenixstick");

    config.AddCommand<DeviceCommand>("device");
    config.AddCommand<InventoryCommand>("inventory");
    config.AddCommand<SnapshotCommand>("snapshot");
    config.AddCommand<DebloatCommand>("debloat");

    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.MarkupLine($"[red]Fehler:[/] {Markup.Escape(ex.Message)}");
        return 1;
    });
});

return await app.RunAsync(args);
