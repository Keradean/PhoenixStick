using System.Text.Json;
using PhoenixStick.Storage;

namespace PhoenixStick.Config;

public static class ConfigLoader
{
    public const string FileName = "config.json";

    /// <summary>Liest config.json. <paramref name="hostOverride"/> (z. B. von --host) hat Vorrang, auch als "host:port".</summary>
    public static AppConfig Load(string path, string? hostOverride)
    {
        AppConfig? fromFile = null;
        if (File.Exists(path))
        {
            fromFile = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), JsonDefaults.Options)
                ?? throw new InvalidDataException($"{path} ist leer oder ungültig.");
        }

        if (!string.IsNullOrWhiteSpace(hostOverride))
            return ParseHost(hostOverride, fromFile?.Port ?? AppConfig.DefaultPort) with { Keep = fromFile?.Keep, RestrictBackground = fromFile?.RestrictBackground };

        if (fromFile is null)
            throw new FileNotFoundException(
                $"{path} fehlt. Vorlage kopieren (cp config.example.json config.json) oder --host angeben.");

        if (string.IsNullOrWhiteSpace(fromFile.Host))
            throw new InvalidDataException($"In {path} fehlt \"host\".");

        return fromFile with { Port = fromFile.Port == 0 ? AppConfig.DefaultPort : fromFile.Port };
    }

    private static AppConfig ParseHost(string value, int defaultPort)
    {
        var separator = value.LastIndexOf(':');
        if (separator > 0 && int.TryParse(value[(separator + 1)..], out var port))
            return new AppConfig(value[..separator], port);

        return new AppConfig(value, defaultPort);
    }
}
