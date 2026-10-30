using PhoenixStick.Adb;
using PhoenixStick.Changes;
using PhoenixStick.Tests.Fakes;

namespace PhoenixStick.Tests.Changes;

public sealed class PackageChangerTests : IDisposable
{
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), "phoenixstick-changes", Guid.NewGuid() + ".json");

    public void Dispose()
    {
        if (File.Exists(_logPath))
            File.Delete(_logPath);
    }

    private static int CountEntriesOnDisk(string path) => File.Exists(path) ? ChangeLog.Load(path).Entries.Count : 0;

    [Fact]
    public async Task Disable_ChangesAndLogsEachPackage()
    {
        var device = new SimulatedDevice();
        var log = ChangeLog.Load(_logPath);

        var summary = await new PackageChanger(device, log).RunAsync(ChangeAction.Disable, ["com.amazon.a", "com.amazon.b"]);

        Assert.Equal(new ChangeSummary(2, 0, 0, 0, Stopped: false), summary);
        Assert.Equal(["com.amazon.a", "com.amazon.b"], device.Disabled.Order());
        var entries = ChangeLog.Load(_logPath).Entries;
        Assert.All(entries, e => Assert.Equal((ChangeAction.Disable, PackageState.Enabled, ChangeResult.Success), (e.Action, e.StateBefore, e.Result)));
        Assert.Contains("new state: disabled-user", entries[0].DeviceMessage);
    }

    [Fact]
    public async Task Log_IsWrittenAfterEachPackage()
    {
        var device = new SimulatedDevice();
        var countsBeforeWrite = new List<int>();
        device.BeforeWrite = _ => countsBeforeWrite.Add(CountEntriesOnDisk(_logPath));

        await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.a", "com.amazon.b", "com.amazon.c"]);

        // Vor dem n-ten Schreibbefehl stehen schon n-1 Einträge auf der Platte
        Assert.Equal([0, 1, 2], countsBeforeWrite);
    }

    [Fact]
    public async Task UnexpectedError_StopsRun_AndKeepsEarlierEntries()
    {
        var device = new SimulatedDevice();
        device.FailingPackages.Add("com.amazon.b");

        var summary = await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.a", "com.amazon.b", "com.amazon.c"]);

        Assert.True(summary.Stopped);
        Assert.Equal(1, summary.Failed);
        Assert.DoesNotContain(device.WriteCalls, c => c.EndsWith("com.amazon.c"));
        var entries = ChangeLog.Load(_logPath).Entries;
        Assert.Equal(["com.amazon.a", "com.amazon.b"], entries.Select(e => e.Package));
        Assert.Equal([ChangeResult.Success, ChangeResult.Failed], entries.Select(e => e.Result));
        Assert.Contains("Unerwartet", entries[1].DeviceMessage);
    }

    [Fact]
    public async Task ProtectedPackage_IsSkippedAndLogged_RunContinues()
    {
        var device = new SimulatedDevice();
        device.ProtectedPackages.Add("com.amazon.device.metrics");

        var summary = await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.device.metrics", "com.amazon.a"]);

        Assert.Equal(new ChangeSummary(1, 0, 1, 0, Stopped: false), summary);
        Assert.Equal(ChangeResult.Protected, ChangeLog.Load(_logPath).Entries[0].Result);
    }

    [Fact]
    public async Task StateNotChangedDespiteSuccessMessage_CountsAsFailure()
    {
        var device = new SimulatedDevice();
        device.SilentlyIgnored.Add("com.amazon.a");

        var summary = await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.a", "com.amazon.b"]);

        Assert.True(summary.Stopped);
        Assert.Contains("Zustand danach", ChangeLog.Load(_logPath).Entries[0].DeviceMessage);
    }

    [Fact]
    public async Task AlreadyDisabled_IsNotTouchedAndNotLogged()
    {
        var device = new SimulatedDevice();
        device.Disabled.Add("com.amazon.a");

        var summary = await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.a"]);

        Assert.Equal(1, summary.Skipped);
        Assert.Empty(device.WriteCalls);
        Assert.False(File.Exists(_logPath));
    }

    [Fact]
    public async Task WithoutApply_WriteIsBlocked_AndRunStops()
    {
        var device = new SimulatedDevice(applyEnabled: false);

        var summary = await new PackageChanger(device, ChangeLog.Load(_logPath)).RunAsync(ChangeAction.Disable, ["com.amazon.a"]);

        Assert.True(summary.Stopped);
        Assert.Empty(device.WriteCalls);
        Assert.Empty(device.Disabled);
        Assert.Contains("--apply", ChangeLog.Load(_logPath).Entries[0].DeviceMessage);
    }
}
