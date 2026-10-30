using PhoenixStick.Parsing;

namespace PhoenixStick.Tests.Parsing;

// Beispielzeilen aus backup/ausgangszustand/
public class PackageListParserTests
{
    [Fact]
    public void Parse_WithPaths_ReturnsPackageNames()
    {
        const string output = """
            package:/system/priv-app/CtsShimPrivPrebuilt/CtsShimPrivPrebuilt.apk=com.android.cts.priv.ctsshim
            package:/data/app/com.amazon.alexamediaplayer.runtime.ftv-2/base.apk=com.amazon.alexamediaplayer.runtime.ftv
            package:/system/priv-app/com.amazon.net.smartconnect/com.amazon.net.smartconnect.apk=com.amazon.net.smartconnect
            """;

        var packages = PackageListParser.Parse(output);

        Assert.Equal(
            ["com.android.cts.priv.ctsshim", "com.amazon.alexamediaplayer.runtime.ftv", "com.amazon.net.smartconnect"],
            packages);
    }

    [Fact]
    public void Parse_SystemList_ReturnsPackageNames()
    {
        const string output = "package:com.android.cts.priv.ctsshim\npackage:com.amazon.alexamediaplayer.runtime.ftv\npackage:com.amazon.net.smartconnect\n";

        Assert.Equal(3, PackageListParser.Parse(output).Count);
    }

    [Fact]
    public void Parse_ThirdPartyListWithCrLf_TrimsLineEndings()
    {
        const string output = "package:com.netflix.ninja\r\npackage:com.disney.disneyplus\r\npackage:com.zdf.android.mediathek\r\n";

        Assert.Equal(["com.netflix.ninja", "com.disney.disneyplus", "com.zdf.android.mediathek"], PackageListParser.Parse(output));
    }

    [Fact]
    public void Parse_EmptyDisabledList_ReturnsEmpty()
    {
        // pakete_deaktiviert.txt ist im Ausgangszustand leer
        Assert.Empty(PackageListParser.Parse(""));
    }

    [Fact]
    public void Parse_IgnoresLinesWithoutPrefix()
    {
        const string output = "WARNING: linker: something\npackage:com.amazon.tv.launcher\n\n";

        Assert.Equal(["com.amazon.tv.launcher"], PackageListParser.Parse(output));
    }
}
