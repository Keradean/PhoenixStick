using System.ComponentModel;
using System.Diagnostics;

namespace PhoenixStick.Adb;

public class AdbClient(string serial, bool applyEnabled, TimeSpan? timeout = null) : IAdbClient
{
    private readonly bool _applyEnabled = applyEnabled;
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(20);

    public string Serial { get; } = serial;

    public Task<AdbResult> ConnectAsync(CancellationToken ct = default) =>
        RunAsync(["connect", Serial], ct);

    public Task<AdbResult> DevicesAsync(CancellationToken ct = default) =>
        RunAsync(["devices"], ct);

    public Task<AdbResult> ShellReadAsync(string command, CancellationToken ct = default)
    {
        if (!AllowedCommands.IsAllowedRead(command))
            throw new ArgumentException($"Kein erlaubter Lesebefehl: {command}", nameof(command));

        return RunAsync(["-s", Serial, "shell", command], ct);
    }

    public Task<AdbResult> ShellWriteAsync(string command, CancellationToken ct = default)
    {
        if (!_applyEnabled)
            throw new ApplyRequiredException(command);

        if (!AllowedCommands.IsAllowedWrite(command))
            throw new ArgumentException($"Kein erlaubter Schreibbefehl: {command}", nameof(command));

        return RunAsync(["-s", Serial, "shell", command], ct);
    }

    private async Task<AdbResult> RunAsync(string[] args, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo("adb")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new AdbNotFoundException(ex);
        }

        // Beide Streams parallel lesen, sonst kann adb blockieren, wenn ein Puffer voll ist
        var stdOutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stdErrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            if (ct.IsCancellationRequested)
                throw;
            throw new TimeoutException(
                $"adb hat nach {_timeout.TotalSeconds:0} Sekunden nicht geantwortet: adb {string.Join(' ', args)}");
        }

        return new AdbResult(process.ExitCode, await stdOutTask, await stdErrTask);
    }
}
