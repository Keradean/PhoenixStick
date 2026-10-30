using PhoenixStick.Adb;

namespace PhoenixStick.Tests.Fakes;

/// <summary>
/// Simuliert einen Stick mit Zustand: disable/enable ändern, was "pm list packages -d" liefert.
/// Prüft Lese- und Schreibbefehle mit denselben Regeln wie der echte AdbClient.
/// </summary>
public class SimulatedDevice(bool applyEnabled = true) : IAdbClient
{
    private readonly bool _applyEnabled = applyEnabled;

    public string Serial => "192.0.2.1:5555";

    public HashSet<string> Disabled { get; } = [];

    public HashSet<string> ProtectedPackages { get; } = [];

    /// <summary>Pakete, bei denen das Gerät einen unerwarteten Fehler meldet.</summary>
    public HashSet<string> FailingPackages { get; } = [];

    /// <summary>Pakete, bei denen pm "Erfolg" meldet, der Zustand sich aber nicht ändert.</summary>
    public HashSet<string> SilentlyIgnored { get; } = [];

    public List<string> WriteCalls { get; } = [];

    /// <summary>Wird vor jedem Schreibbefehl aufgerufen, z. B. um das Protokoll zu prüfen.</summary>
    public Action<string>? BeforeWrite { get; set; }

    public Task<AdbResult> ConnectAsync(CancellationToken ct = default) => Ok("");

    public Task<AdbResult> DevicesAsync(CancellationToken ct = default) => Ok($"List of devices attached\n{Serial}\tdevice\n");

    public Task<AdbResult> ShellReadAsync(string command, CancellationToken ct = default)
    {
        if (!AllowedCommands.IsAllowedRead(command))
            throw new ArgumentException($"Kein erlaubter Lesebefehl: {command}");

        if (command == "pm list packages -d")
            return Ok(string.Concat(Disabled.Select(p => $"package:{p}\n")));

        throw new InvalidOperationException($"Nicht simuliert: {command}");
    }

    public Task<AdbResult> ShellWriteAsync(string command, CancellationToken ct = default)
    {
        if (!_applyEnabled)
            throw new ApplyRequiredException(command);
        if (!AllowedCommands.IsAllowedWrite(command))
            throw new ArgumentException($"Kein erlaubter Schreibbefehl: {command}");

        BeforeWrite?.Invoke(command);
        WriteCalls.Add(command);

        var package = command.Split(' ')[^1];
        if (FailingPackages.Contains(package))
            return Task.FromResult(new AdbResult(255, "", "Error: java.lang.IllegalStateException: Unerwartet"));
        if (SilentlyIgnored.Contains(package))
            return Ok($"Package {package} new state: disabled-user\n");

        if (command.StartsWith("pm disable-user", StringComparison.Ordinal))
        {
            if (ProtectedPackages.Contains(package))
                return Task.FromResult(new AdbResult(255, "", "Error: java.lang.SecurityException: Cannot disable a protected package: " + package));
            Disabled.Add(package);
            return Ok($"Package {package} new state: disabled-user\n");
        }

        Disabled.Remove(package);
        return Ok($"Package {package} new state: enabled\n");
    }

    private static Task<AdbResult> Ok(string stdOut) => Task.FromResult(new AdbResult(0, stdOut, ""));
}
