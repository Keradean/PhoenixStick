using PhoenixStick.Catalog;
using static PhoenixStick.Tests.Catalog.TempCatalog;

namespace PhoenixStick.Tests.Catalog;

public class CatalogLoaderTests
{
    [Fact]
    public void Load_ReadsCategoryAsEnum()
    {
        using var dir = new TempCatalog(List(Entry("com.amazon.a", "safe"), Entry("com.amazon.b", "risky", group: "alexa")));

        var catalog = CatalogLoader.Load(dir.Directory);

        Assert.Equal(Category.Safe, catalog.Find("com.amazon.a")!.Category);
        Assert.Equal(Category.Risky, catalog.Find("com.amazon.b")!.Category);
        Assert.Equal("alexa", catalog.Find("com.amazon.b")!.Group);
        Assert.Empty(catalog.Warnings);
    }

    [Fact]
    public void Load_NeverTouchWinsOverSafe()
    {
        using var dir = new TempCatalog(
            List(Entry("com.amazon.tv.launcher", "safe"), Entry("com.amazon.ads", "safe")),
            NeverTouch("com.amazon.tv.launcher"));

        var catalog = CatalogLoader.Load(dir.Directory);

        Assert.Null(catalog.Find("com.amazon.tv.launcher"));
        Assert.DoesNotContain(catalog.Entries, e => e.Package == "com.amazon.tv.launcher");
        Assert.True(catalog.IsNeverTouch("com.amazon.tv.launcher"));
        Assert.Contains(catalog.Warnings, w => w.Contains("com.amazon.tv.launcher"));
    }

    [Fact]
    public void Load_Duplicate_WarnsAndKeepsFirst()
    {
        using var dir = new TempCatalog(List(Entry("com.amazon.a", "safe"), Entry("com.amazon.a", "risky")));

        var catalog = CatalogLoader.Load(dir.Directory);

        Assert.Single(catalog.Entries);
        Assert.Equal(Category.Safe, catalog.Find("com.amazon.a")!.Category);
        Assert.Contains(catalog.Warnings, w => w.Contains("doppelt"));
    }

    [Theory]
    [InlineData("", "leer")]
    [InlineData("   ", "leer")]
    [InlineData("[ { \"package\": ", "kein gültiges JSON")]
    [InlineData("{ \"package\": \"com.amazon.a\" }", "kein gültiges JSON")]
    [InlineData("[ { \"package\": \"com.amazon.a\", \"category\": \"sicher\", \"reason\": \"x\" } ]", "kein gültiges JSON")]
    [InlineData("[ { \"package\": \"com.amazon.a\", \"category\": 0, \"reason\": \"x\" } ]", "kein gültiges JSON")]
    [InlineData("[ { \"package\": \"com.amazon.a\", \"reason\": \"x\" } ]", "\"category\" fehlt")]
    [InlineData("[ { \"category\": \"safe\", \"reason\": \"x\" } ]", "\"package\" fehlt")]
    [InlineData("null", "keine Liste")]
    public void Load_BrokenCatalog_ThrowsReadableError(string json, string expectedText)
    {
        using var dir = new TempCatalog(json);

        var ex = Assert.Throws<CatalogException>(() => CatalogLoader.Load(dir.Directory));

        Assert.Contains(expectedText, ex.Message);
        Assert.Contains("fireos.json", ex.Message);
    }

    [Fact]
    public void Load_BrokenNeverTouch_ThrowsReadableError()
    {
        using var dir = new TempCatalog(List(Entry("com.amazon.a", "safe")), neverTouchJson: "");

        var ex = Assert.Throws<CatalogException>(() => CatalogLoader.Load(dir.Directory));

        Assert.Contains("never-touch.json", ex.Message);
    }

    [Fact]
    public void Load_MissingDirectory_ThrowsReadableError()
    {
        var ex = Assert.Throws<CatalogException>(() => CatalogLoader.Load("/gibt/es/nicht"));

        Assert.Contains("fehlt", ex.Message);
    }
}
