using PhoenixStick.Adb;
using PhoenixStick.Catalog;
using PhoenixStick.Changes;
using PhoenixStick.Commands;
using PhoenixStick.Inventory;
using PhoenixStick.Parsing;
using PhoenixStick.Tests.Catalog;
using PhoenixStick.Tests.Fakes;

namespace PhoenixStick.Tests.Changes;

public sealed class BackgroundTests : IDisposable
{
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), "phoenixstick-changes", Guid.NewGuid() + ".json");

    public void Dispose()
    {
        if (File.Exists(_logPath))
            File.Delete(_logPath);
    }

    [Theory]
    [InlineData("appops set com.netflix.ninja RUN_IN_BACKGROUND ignore", true)]
    [InlineData("appops set com.netflix.ninja RUN_IN_BACKGROUND allow", true)]
    [InlineData("appops set com.netflix.ninja RUN_IN_BACKGROUND deny", false)]
    [InlineData("appops set com.netflix.ninja CAMERA ignore", false)]
    [InlineData("appops set com.netflix.ninja RUN_IN_BACKGROUND ignore; reboot", false)]
    [InlineData("appops reset com.netflix.ninja", false)]
    public void Whitelist_OnlyAllowsBackgroundOp(string command, bool expected) =>
        Assert.Equal(expected, AllowedCommands.IsAllowedWrite(command));

    [Fact]
    public void Parser_FindsRestrictedPackages_IgnoresUidLevelOps()
    {
        // Aufbau wie auf dem Stick, mode=1 heißt ignore
        const string output = """
            Current AppOps Service state:
              Uid u0a216:
                RUN_IN_BACKGROUND: mode=1
                Package com.netflix.ninja:
                  WAKE_LOCK: mode=0; time=+11m56s305ms ago
                  RUN_IN_BACKGROUND: mode=0; time=+6m50s315ms ago
              Uid u0a217:
                Package tv.twitch.android.viewer:
                  RUN_IN_BACKGROUND: mode=1; time=+1m ago
            """;

        Assert.Equal(["tv.twitch.android.viewer"], AppOpsParser.BackgroundRestricted(output));
    }

    [Fact]
    public async Task Restrict_ThenRestore_UsesLogAndVerifies()
    {
        var device = new SimulatedDevice();
        var log = ChangeLog.Load(_logPath);

        var summary = await new PackageChanger(device, log).RunAsync(ChangeAction.RestrictBackground, ["com.netflix.ninja"]);

        Assert.Equal(1, summary.Changed);
        Assert.Contains("com.netflix.ninja", device.BackgroundRestricted);
        Assert.Equal(["com.netflix.ninja"], log.PackagesRestrictedByTool());
        Assert.Empty(log.PackagesDisabledByTool());

        var report = ToolState.CompareBackground(log, device.BackgroundRestricted);
        await new PackageChanger(device, log).RunAsync(ChangeAction.AllowBackground, report.StillApplied);

        Assert.Empty(device.BackgroundRestricted);
        Assert.Empty(log.PackagesRestrictedByTool());
        Assert.Equal("appops set com.netflix.ninja RUN_IN_BACKGROUND allow", device.WriteCalls[^1]);
    }

    [Fact]
    public void Check_DetectsLostRestriction()
    {
        var log = ChangeLog.Load(_logPath);
        log.Append(new ChangeEntry(DateTime.Now, "com.netflix.ninja", ChangeAction.RestrictBackground, PackageState.Enabled, ChangeResult.Success, ""));

        var report = ToolState.CompareBackground(log, new HashSet<string>());

        Assert.Equal(["com.netflix.ninja"], report.Reverted);
    }

    [Fact]
    public void Check_UninstalledApp_IsNotReportedAsReverted()
    {
        var log = ChangeLog.Load(_logPath);
        log.Append(new ChangeEntry(DateTime.Now, "de.prosiebensat1digital.seventv", ChangeAction.RestrictBackground, PackageState.Enabled, ChangeResult.Success, ""));
        log.Append(new ChangeEntry(DateTime.Now, "com.netflix.ninja", ChangeAction.RestrictBackground, PackageState.Enabled, ChangeResult.Success, ""));

        var report = ToolState.CompareBackground(
            log,
            restrictedOnDevice: new HashSet<string> { "com.netflix.ninja" },
            installed: new HashSet<string> { "com.netflix.ninja" });

        Assert.Empty(report.Reverted);
        Assert.Equal(["com.netflix.ninja"], report.StillApplied);
        Assert.Equal(["de.prosiebensat1digital.seventv"], report.Removed);
    }

    [Fact]
    public void Planner_OnlyThirdPartyApps()
    {
        using var dir = new TempCatalog("[]", TempCatalog.NeverTouch("com.amazon.tv.launcher"));
        var catalog = CatalogLoader.Load(dir.Directory);
        PackageInfo[] installed =
        [
            new("com.netflix.ninja", true, IsSystem: false),
            new("tv.twitch.android.viewer", true, IsSystem: false),
            new("com.amazon.tv.launcher", true, IsSystem: true),
            new("com.amazon.firebat", true, IsSystem: true),
        ];

        var plan = BackgroundPlanner.Plan(
            ["com.netflix.ninja", "tv.twitch.android.viewer", "com.amazon.tv.launcher", "com.amazon.firebat", "com.gibt.es.nicht"],
            installed,
            catalog,
            new HashSet<string> { "tv.twitch.android.viewer" });

        Assert.Equal(["com.netflix.ninja"], plan.ToRestrict);
        Assert.Equal(
            ["schon gesperrt", "never touch", "Systempaket, nur Drittanbieter-Apps erlaubt", "nicht installiert"],
            plan.Skipped.Select(s => s.Reason));
    }
}
