using PhoenixStick.Changes;
using PhoenixStick.Tests.Fakes;

namespace PhoenixStick.Tests.Changes;

public sealed class ToolStateTests : IDisposable
{
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), "phoenixstick-changes", Guid.NewGuid() + ".json");

    public void Dispose()
    {
        if (File.Exists(_logPath))
            File.Delete(_logPath);
    }

    private static ChangeEntry Entry(string package, ChangeAction action, PackageState before, ChangeResult result = ChangeResult.Success) =>
        new(new DateTime(2026, 10, 30), package, action, before, result, "");

    [Fact]
    public void PackagesDisabledByTool_IgnoresPackagesThatWereAlreadyOff()
    {
        var log = ChangeLog.Load(_logPath);
        log.Append(Entry("com.amazon.byTool", ChangeAction.Disable, PackageState.Enabled));
        log.Append(Entry("com.amazon.wasOff", ChangeAction.Disable, PackageState.Disabled));
        log.Append(Entry("com.amazon.protected", ChangeAction.Disable, PackageState.Enabled, ChangeResult.Protected));
        log.Append(Entry("com.amazon.failed", ChangeAction.Disable, PackageState.Enabled, ChangeResult.Failed));

        Assert.Equal(["com.amazon.byTool"], log.PackagesDisabledByTool());
    }

    [Fact]
    public void PackagesDisabledByTool_RestoredPackageIsGone()
    {
        var log = ChangeLog.Load(_logPath);
        log.Append(Entry("com.amazon.a", ChangeAction.Disable, PackageState.Enabled));
        log.Append(Entry("com.amazon.a", ChangeAction.Enable, PackageState.Disabled));

        Assert.Empty(log.PackagesDisabledByTool());
    }

    [Fact]
    public async Task Restore_OnlyEnablesPackagesTheToolDisabled()
    {
        var device = new SimulatedDevice();
        device.Disabled.UnionWith(["com.amazon.byTool", "com.amazon.wasOffBefore"]);
        var log = ChangeLog.Load(_logPath);
        log.Append(Entry("com.amazon.byTool", ChangeAction.Disable, PackageState.Enabled));

        var report = ToolState.ComparePackages(log, device.Disabled);
        await new PackageChanger(device, log).RunAsync(ChangeAction.Enable, report.StillApplied);

        Assert.Equal(["pm enable --user 0 com.amazon.byTool"], device.WriteCalls);
        Assert.Equal(["com.amazon.wasOffBefore"], device.Disabled);
        Assert.Empty(log.PackagesDisabledByTool());
    }

    [Fact]
    public void Check_DetectsPackageThatIsActiveAgain()
    {
        var log = ChangeLog.Load(_logPath);
        log.Append(Entry("com.amazon.a", ChangeAction.Disable, PackageState.Enabled));
        log.Append(Entry("com.amazon.b", ChangeAction.Disable, PackageState.Enabled));

        // Nach einem Update ist nur noch b aus
        var report = ToolState.ComparePackages(log, new HashSet<string> { "com.amazon.b" });

        Assert.Equal(["com.amazon.a"], report.Reverted);
        Assert.Equal(["com.amazon.b"], report.StillApplied);
    }

    [Fact]
    public void DryRunPaths_NeverCallWrite()
    {
        // Probelauf von restore und check: nur Vergleich, kein Schreibzugriff
        var device = new SimulatedDevice(applyEnabled: false);
        device.Disabled.Add("com.amazon.a");
        var log = ChangeLog.Load(_logPath);
        log.Append(Entry("com.amazon.a", ChangeAction.Disable, PackageState.Enabled));

        ToolState.ComparePackages(log, device.Disabled);

        Assert.Empty(device.WriteCalls);
    }

    [Fact]
    public void Load_CorruptLog_ThrowsAndKeepsFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        File.WriteAllText(_logPath, "[ { kaputt");

        Assert.Throws<InvalidDataException>(() => ChangeLog.Load(_logPath));
        Assert.Equal("[ { kaputt", File.ReadAllText(_logPath));
    }
}
