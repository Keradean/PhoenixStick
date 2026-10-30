using System.ComponentModel;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class ApplySettings : GlobalSettings
{
    [CommandOption("--apply")]
    [Description("Änderungen wirklich ausführen. Ohne diese Option läuft nur ein Probelauf")]
    public bool Apply { get; init; }

    [CommandOption("--package <PAKET>")]
    [Description("Nur dieses eine Paket")]
    public string? Package { get; init; }
}
