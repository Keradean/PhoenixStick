using PhoenixStick.Device;
using PhoenixStick.Parsing;

namespace PhoenixStick.Tests.Parsing;

public class SettingsParserTests
{
    [Fact]
    public void Parse_ReadsRealLines()
    {
        // Aus settings_global.txt und settings_secure.txt
        const string output = "1D_DISAMBIGUATION_PACKAGE=com.amazon.tv.launcher\nDATA_MONITORING_ENABLED=null\nadb_enabled=1\nlimit_ad_tracking=0\n";

        var settings = SettingsParser.Parse(output);

        Assert.Equal(4, settings.Count);
        Assert.Equal("com.amazon.tv.launcher", settings["1D_DISAMBIGUATION_PACKAGE"]);
        Assert.Equal("null", settings["DATA_MONITORING_ENABLED"]);
        Assert.Equal("0", settings["limit_ad_tracking"]);
    }

    [Fact]
    public void Parse_ValueWithEqualsSign_SplitsAtFirst()
    {
        var settings = SettingsParser.Parse("some_url=https://example.com/?a=b\n");

        Assert.Equal("https://example.com/?a=b", settings["some_url"]);
    }

    [Fact]
    public void Parse_MultilineValue_AppendsToPreviousKey()
    {
        var settings = SettingsParser.Parse("first=line one\nline two\nsecond=2\n");

        Assert.Equal("line one\nline two", settings["first"]);
        Assert.Equal("2", settings["second"]);
    }
}

public class DeviceListParserTests
{
    private const string Serial = "192.168.178.26:5555";

    [Theory]
    [InlineData("List of devices attached\n192.168.178.26:5555\tdevice\n\n", DeviceState.Device)]
    [InlineData("List of devices attached\n192.168.178.26:5555\tunauthorized\n", DeviceState.Unauthorized)]
    [InlineData("List of devices attached\n192.168.178.26:5555\toffline\n", DeviceState.Offline)]
    [InlineData("List of devices attached\n192.168.178.26:5555\tno permissions\n", DeviceState.Unknown)]
    [InlineData("List of devices attached\n\n", DeviceState.NotFound)]
    [InlineData("List of devices attached\n192.168.178.99:5555\tdevice\n", DeviceState.NotFound)]
    public void GetState(string output, DeviceState expected) =>
        Assert.Equal(expected, DeviceListParser.GetState(output, Serial));
}
