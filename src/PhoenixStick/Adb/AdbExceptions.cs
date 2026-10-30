namespace PhoenixStick.Adb;

public class ApplyRequiredException(string command)
    : InvalidOperationException($"Schreibender Befehl ohne --apply blockiert: {command}");

public class AdbNotFoundException(Exception inner)
    : Exception("adb wurde nicht gefunden. Installieren mit: sudo pacman -S android-tools", inner);

public class AdbCommandException(string command, AdbResult result)
    : Exception($"adb-Befehl fehlgeschlagen (Exit-Code {result.ExitCode}): {command}\n{result.StdErr.Trim()}")
{
    public AdbResult Result { get; } = result;
}
