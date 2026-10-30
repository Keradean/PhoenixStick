namespace PhoenixStick.Adb;

public interface IAdbClient
{
    /// <summary>Adresse des Geräts im Format host:port.</summary>
    string Serial { get; }

    Task<AdbResult> ConnectAsync(CancellationToken ct = default);

    Task<AdbResult> DevicesAsync(CancellationToken ct = default);

    /// <summary>Führt einen Lesebefehl auf dem Gerät aus. Erlaubt sind nur Befehle aus <see cref="AllowedCommands"/>.</summary>
    Task<AdbResult> ShellReadAsync(string command, CancellationToken ct = default);

    /// <summary>Einziger Weg, das Gerät zu ändern. Wirft ohne --apply eine <see cref="ApplyRequiredException"/>.</summary>
    Task<AdbResult> ShellWriteAsync(string command, CancellationToken ct = default);
}
