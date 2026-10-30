namespace PhoenixStick.Config;

public record AppConfig(string Host, int Port)
{
    public const int DefaultPort = 5555;

    public string Serial => $"{Host}:{Port}";
}
