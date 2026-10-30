using PhoenixStick.Catalog;

namespace PhoenixStick.Tests.Catalog;

/// <summary>Prüft die echten Dateien in catalog/.</summary>
public class RealCatalogTests
{
    private static string CatalogDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PhoenixStick.sln")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("PhoenixStick.sln nicht gefunden"), "catalog");
    }

    private static readonly PackageCatalog _catalog = CatalogLoader.Load(CatalogDirectory());

    [Fact]
    public void RealCatalog_LoadsWithoutWarnings() => Assert.Empty(_catalog.Warnings);

    [Theory]
    [InlineData("android")]
    [InlineData("com.android.systemui")]
    [InlineData("com.android.providers.settings")]
    [InlineData("com.android.providers.downloads")]
    [InlineData("com.amazon.tv.launcher")]
    [InlineData("com.amazon.firehomestarter")]
    [InlineData("com.amazon.tv.settings.v2")]
    [InlineData("com.android.shell")]
    [InlineData("com.android.bluetooth")]
    [InlineData("com.amazon.device.software.ota")]
    [InlineData("com.amazon.firebat")]
    [InlineData("com.amazon.device.messaging")]
    [InlineData("com.amazon.tv.devicecontrol")]
    public void RealCatalog_NeverTouchContains(string package)
    {
        Assert.True(_catalog.IsNeverTouch(package));
        Assert.Null(_catalog.Find(package));
    }

    [Fact]
    public void RealCatalog_AlexaAndMirroringAreNotSafe() =>
        Assert.All(_catalog.Entries.Where(e => e.Group is not null), e => Assert.NotEqual(Category.Safe, e.Category));
}
