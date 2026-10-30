namespace PhoenixStick.Config;

/// <param name="Keep">Pakete, die auf diesem Gerät bleiben sollen, auch wenn das Preset sie enthält.</param>
/// <param name="RestrictBackground">Drittanbieter-Apps, die nicht im Hintergrund laufen sollen.</param>
public record AppConfig(string Host, int Port, IReadOnlyList<string>? Keep = null, IReadOnlyList<string>? RestrictBackground = null)
{
    public const int DefaultPort = 5555;

    public string Serial => $"{Host}:{Port}";

    public IReadOnlyList<string> KeepPackages => Keep ?? [];

    public IReadOnlyList<string> BackgroundRestricted => RestrictBackground ?? [];
}
