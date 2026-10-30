using PhoenixStick.Adb;

namespace PhoenixStick.Tests.Fakes;

/// <summary>Ersetzt das Gerät in Tests. Antworten werden pro Shell-Befehl vorbereitet.</summary>
public class FakeAdbClient(string serial = "192.168.0.10:5555", bool applyEnabled = false) : IAdbClient
{
    private readonly Dictionary<string, AdbResult> _responses = new(StringComparer.Ordinal);
    private readonly bool _applyEnabled = applyEnabled;

    public string Serial { get; } = serial;

    public string DevicesOutput { get; set; } = "";

    public List<string> ShellCalls { get; } = [];

    public List<string> WriteCalls { get; } = [];

    public FakeAdbClient Respond(string command, string stdOut, int exitCode = 0, string stdErr = "")
    {
        _responses[command] = new AdbResult(exitCode, stdOut, stdErr);
        return this;
    }

    public Task<AdbResult> ConnectAsync(CancellationToken ct = default) =>
        Task.FromResult(new AdbResult(0, $"already connected to {Serial}\n", ""));

    public Task<AdbResult> DevicesAsync(CancellationToken ct = default) =>
        Task.FromResult(new AdbResult(0, DevicesOutput, ""));

    public Task<AdbResult> ShellReadAsync(string command, CancellationToken ct = default)
    {
        if (!AllowedCommands.IsAllowedRead(command))
            throw new ArgumentException($"Kein erlaubter Lesebefehl: {command}", nameof(command));

        ShellCalls.Add(command);
        if (!_responses.TryGetValue(command, out var result))
            throw new InvalidOperationException($"Keine vorbereitete Antwort für: {command}");
        return Task.FromResult(result);
    }

    public Task<AdbResult> ShellWriteAsync(string command, CancellationToken ct = default)
    {
        if (!_applyEnabled)
            throw new ApplyRequiredException(command);

        WriteCalls.Add(command);
        return Task.FromResult(new AdbResult(0, "", ""));
    }
}
