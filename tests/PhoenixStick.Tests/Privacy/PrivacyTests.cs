using PhoenixStick.Catalog;
using PhoenixStick.Changes;
using PhoenixStick.Commands;
using PhoenixStick.Privacy;
using PhoenixStick.Tests.Fakes;

namespace PhoenixStick.Tests.Privacy;

public sealed class PrivacyTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "phoenixstick-privacy", Guid.NewGuid().ToString());

    private string LogPath => Path.Combine(_dir, "changes.json");

    public PrivacyTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static SettingTarget Target(string id, string value) => SettingTarget.FromId(id, value);

    [Fact]
    public async Task Put_ReadsOldValue_SetsAndLogs()
    {
        var device = new SimulatedDevice();
        device.Settings["secure/limit_ad_tracking"] = "0";
        var log = ChangeLog.Load(LogPath);

        var summary = await new SettingsChanger(device, log).RunAsync(ChangeAction.PutSetting, [Target("secure/limit_ad_tracking", "1")]);

        Assert.Equal(1, summary.Changed);
        Assert.Equal("1", device.Settings["secure/limit_ad_tracking"]);
        var entry = ChangeLog.Load(LogPath).Entries.Single();
        Assert.Equal(("0", "1"), (entry.OldValue, entry.NewValue));
        Assert.Equal([new SettingChange("secure/limit_ad_tracking", "0", "1")], log.SettingsChangedByTool());
    }

    [Fact]
    public async Task MissingKey_IsNeverCreated()
    {
        var device = new SimulatedDevice();

        var summary = await new SettingsChanger(device, ChangeLog.Load(LogPath)).RunAsync(
            ChangeAction.PutSetting, [Target("global/amazon:device_metrics_opt_in", "0")]);

        Assert.Equal(1, summary.Skipped);
        Assert.Empty(device.WriteCalls);
        Assert.False(device.Settings.ContainsKey("global/amazon:device_metrics_opt_in"));
    }

    [Fact]
    public async Task ValueNotTaken_StopsAndLogsFailure()
    {
        var device = new SimulatedDevice();
        device.Settings["secure/a"] = "0";
        device.Settings["secure/b"] = "0";
        device.StubbornSettings.Add("secure/a");

        var summary = await new SettingsChanger(device, ChangeLog.Load(LogPath)).RunAsync(
            ChangeAction.PutSetting, [Target("secure/a", "1"), Target("secure/b", "1")]);

        Assert.True(summary.Stopped);
        Assert.Equal("0", device.Settings["secure/b"]);
        Assert.Equal(ChangeResult.Failed, ChangeLog.Load(LogPath).Entries.Single().Result);
    }

    [Fact]
    public async Task Restore_PutsOriginalValueBack()
    {
        var device = new SimulatedDevice();
        device.Settings["secure/USAGE_METRICS_UPLOAD_ENABLED"] = "1";
        var log = ChangeLog.Load(LogPath);
        await new SettingsChanger(device, log).RunAsync(ChangeAction.PutSetting, [Target("secure/USAGE_METRICS_UPLOAD_ENABLED", "0")]);

        var report = ToolState.CompareSettings(log, new Dictionary<string, string> { ["secure/USAGE_METRICS_UPLOAD_ENABLED"] = "0" });
        var targets = report.StillApplied.Select(c => Target(c.Id, c.OriginalValue)).ToList();
        await new SettingsChanger(device, log).RunAsync(ChangeAction.RestoreSetting, targets);

        Assert.Equal("1", device.Settings["secure/USAGE_METRICS_UPLOAD_ENABLED"]);
        Assert.Empty(log.SettingsChangedByTool());
    }

    [Fact]
    public void Check_DetectsSettingResetByFireOs()
    {
        var log = ChangeLog.Load(LogPath);
        log.Append(new ChangeEntry(DateTime.Now, "secure/limit_ad_tracking", ChangeAction.PutSetting, PackageState.Enabled, ChangeResult.Success, "", "0", "1"));

        var report = ToolState.CompareSettings(log, new Dictionary<string, string> { ["secure/limit_ad_tracking"] = "0" });

        Assert.Equal(["secure/limit_ad_tracking"], report.Reverted.Select(c => c.Id));
    }

    [Fact]
    public void Planner_VerifyEntriesAreNeverWritten()
    {
        var set = new PrivacyEntry("secure", "limit_ad_tracking", "1", "x", "", "", PrivacyMode.Set);
        var verify = new PrivacyEntry("secure", "settingsApp:appUsageData:value", "0", "x", "", "", PrivacyMode.Verify);
        var missing = new PrivacyEntry("global", "amazon:acr_enabled", "0", "x", "", "", PrivacyMode.Set);

        var plan = PrivacyPlanner.Plan([(set, "0"), (verify, "1"), (missing, "null")]);

        Assert.Equal([set], plan.ToSet);
        Assert.Equal([verify], plan.MenuOpen);
        Assert.Equal(1, plan.Skipped);
    }

    [Theory]
    [InlineData("""[ { "table": "secure", "key": "a b", "value": "1", "mode": "set" } ]""", "key")]
    [InlineData("""[ { "table": "secure", "key": "a", "value": "1; reboot", "mode": "set" } ]""", "value")]
    [InlineData("""[ { "table": "foo", "key": "a", "value": "1", "mode": "set" } ]""", "table")]
    [InlineData("""[ { "table": "secure", "key": "a", "value": "1" } ]""", "mode")]
    [InlineData("", "leer")]
    public void Catalog_RejectsUnsafeOrBrokenEntries(string json, string expected)
    {
        File.WriteAllText(Path.Combine(_dir, PrivacyCatalog.FileName), json);

        var ex = Assert.Throws<CatalogException>(() => PrivacyCatalog.Load(_dir));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public void RealPrivacyCatalog_Loads()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PhoenixStick.sln")))
            dir = dir.Parent;

        var entries = PrivacyCatalog.Load(Path.Combine(dir!.FullName, "catalog"));

        Assert.Equal(8, entries.Count);
        Assert.Equal(3, entries.Count(e => e.Mode == PrivacyMode.Verify));
    }
}
