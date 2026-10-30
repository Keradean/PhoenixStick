namespace PhoenixStick.Adb;

public record AdbResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Success => ExitCode == 0;
}
