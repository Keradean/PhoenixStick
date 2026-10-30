using PhoenixStick.Config;

namespace PhoenixStick.Tests.Config;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"phoenixstick-config-{Guid.NewGuid()}.json");

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void Load_ReadsFile()
    {
        File.WriteAllText(_path, """{ "host": "192.168.178.26", "port": 5555 }""");

        Assert.Equal("192.168.178.26:5555", ConfigLoader.Load(_path, null).Serial);
    }

    [Fact]
    public void Load_HostOverride_WinsOverFile()
    {
        File.WriteAllText(_path, """{ "host": "192.168.178.26", "port": 5556 }""");

        Assert.Equal("10.0.0.5:5556", ConfigLoader.Load(_path, "10.0.0.5").Serial);
    }

    [Fact]
    public void Load_HostOverrideWithPort()
    {
        Assert.Equal("10.0.0.5:5557", ConfigLoader.Load(_path, "10.0.0.5:5557").Serial);
    }

    [Fact]
    public void Load_NoFileNoOverride_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.Load(_path, null));
    }
}
