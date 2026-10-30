using PhoenixStick.Device;
using PhoenixStick.Parsing;

namespace PhoenixStick.Tests.Parsing;

// Beispielzeilen aus backup/ausgangszustand/getprop.txt
public class GetpropParserTests
{
    private const string Output = """
        [init.svc.amazonfiled]: [running]
        [ro.build.display.id]: [NS6711]
        [ro.build.lab126.build]: [5908]
        [ro.build.lab126.project]: [mantis_fireos_ship_6711]
        [ro.build.mktg.fireos]: [Fire OS 6.7.1.1]
        [ro.build.version.fireos]: [6.0]
        [ro.build.version.name]: [Fire OS 6.7.1.1 (NS6711/5908)]
        [ro.build.version.release]: [7.1.2]
        [ro.build.version.sdk]: [25]
        [ro.build.version.security_patch]: [2025-11-01]
        [ro.product.device]: [mantis]
        [ro.product.manufacturer]: [Amazon]
        [ro.product.model]: [AFTMM]
        [ro.build.version.preview_sdk]: [0]
        [persist.sys.empty]: []
        """;

    [Fact]
    public void Parse_ReadsKeysAndValues()
    {
        var props = GetpropParser.Parse(Output);

        Assert.Equal("AFTMM", props["ro.product.model"]);
        Assert.Equal("7.1.2", props["ro.build.version.release"]);
        Assert.Equal("25", props["ro.build.version.sdk"]);
        Assert.Equal("Fire OS 6.7.1.1 (NS6711/5908)", props["ro.build.version.name"]);
        Assert.Equal("", props["persist.sys.empty"]);
    }

    [Fact]
    public void Parse_HandlesCrLf()
    {
        var props = GetpropParser.Parse("[ro.product.model]: [AFTMM]\r\n");

        Assert.Equal("AFTMM", props["ro.product.model"]);
    }

    [Fact]
    public void FireOs_SelectsFireOsKeysOnly()
    {
        var keys = DeviceInfo.FireOs(GetpropParser.Parse(Output)).Select(p => p.Key).ToList();

        Assert.Equal(
            [
                "ro.build.display.id",
                "ro.build.lab126.build",
                "ro.build.lab126.project",
                "ro.build.mktg.fireos",
                "ro.build.version.fireos",
                "ro.build.version.name",
            ],
            keys);
    }
}
