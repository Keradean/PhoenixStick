using PhoenixStick.Adb;
using PhoenixStick.Inventory;
using PhoenixStick.Snapshot;
using PhoenixStick.Storage;
using PhoenixStick.Tests.Fakes;

namespace PhoenixStick.Tests.Inventory;

public class InventoryReaderTests
{
    private static FakeAdbClient CreateFake() => new FakeAdbClient()
        .Respond("pm list packages", "package:com.amazon.tv.launcher\npackage:com.android.systemui\npackage:com.netflix.ninja\npackage:com.amazon.example.ads\n")
        .Respond("pm list packages -d", "package:com.amazon.example.ads\n")
        .Respond("pm list packages -s", "package:com.amazon.tv.launcher\npackage:com.android.systemui\npackage:com.amazon.example.ads\n");

    [Fact]
    public async Task ReadAsync_CombinesStatusAndType()
    {
        var packages = await new InventoryReader(CreateFake()).ReadAsync();

        Assert.Equal(4, packages.Count);
        Assert.Equal(new PackageInfo("com.amazon.example.ads", IsEnabled: false, IsSystem: true), packages.Single(p => p.Name == "com.amazon.example.ads"));
        Assert.Equal(new PackageInfo("com.netflix.ninja", IsEnabled: true, IsSystem: false), packages.Single(p => p.Name == "com.netflix.ninja"));
    }

    [Theory]
    [InlineData("com.amazon.tv.launcher", PackageInfo.AmazonGroup)]
    [InlineData("com.android.systemui", PackageInfo.AndroidGroup)]
    [InlineData("com.netflix.ninja", PackageInfo.OtherGroup)]
    [InlineData("android", PackageInfo.OtherGroup)]
    public void Group_ByPrefix(string name, string expected) =>
        Assert.Equal(expected, new PackageInfo(name, true, true).Group);

    [Fact]
    public async Task ReadAsync_FailingCommand_Throws()
    {
        var fake = CreateFake().Respond("pm list packages -d", "", exitCode: 1, stdErr: "error");

        await Assert.ThrowsAsync<AdbCommandException>(() => new InventoryReader(fake).ReadAsync());
    }

    [Fact]
    public async Task Snapshot_WritesPackagesAndSettingsWithoutWriting()
    {
        var fake = CreateFake()
            .Respond("settings list global", "adb_enabled=1\n")
            .Respond("settings list secure", "limit_ad_tracking=0\n")
            .Respond("settings list system", "screen_brightness=102\n");
        var root = Path.Combine(Path.GetTempPath(), "phoenixstick-tests", Guid.NewGuid().ToString());

        try
        {
            var summary = await new SnapshotWriter(fake, new StatePaths(root)).WriteAsync(new DateTime(2026, 10, 30, 12, 0, 0));

            Assert.Equal(Path.Combine(root, "snapshots", "2026-10-30_12-00-00"), summary.Directory);
            Assert.True(File.Exists(Path.Combine(summary.Directory, "packages.json")));
            Assert.Contains("limit_ad_tracking", File.ReadAllText(Path.Combine(summary.Directory, "settings_secure.json")));
            Assert.Empty(fake.WriteCalls);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
