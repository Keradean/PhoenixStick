using System.ComponentModel;
using Spectre.Console.Cli;

namespace PhoenixStick.Commands;

public class GlobalSettings : CommandSettings
{
    [CommandOption("--host <HOST>")]
    [Description("IP des Sticks, optional mit Port (192.168.178.26:5555). Überschreibt config.json")]
    public string? Host { get; init; }
}
