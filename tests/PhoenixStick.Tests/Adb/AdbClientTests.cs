using PhoenixStick.Adb;

namespace PhoenixStick.Tests.Adb;

// Die Prüfungen greifen, bevor ein Prozess gestartet wird. Diese Tests rufen adb also nie auf
public class AdbClientTests
{
    [Fact]
    public async Task ShellWriteAsync_WithoutApply_Throws()
    {
        var adb = new AdbClient("192.0.2.1:5555", applyEnabled: false);

        var ex = await Assert.ThrowsAsync<ApplyRequiredException>(
            () => adb.ShellWriteAsync("pm disable-user --user 0 com.amazon.example"));

        Assert.Contains("--apply", ex.Message);
    }

    [Fact]
    public async Task ShellWriteAsync_WithApplyButForbiddenCommand_Throws()
    {
        var adb = new AdbClient("192.0.2.1:5555", applyEnabled: true);

        await Assert.ThrowsAsync<ArgumentException>(() => adb.ShellWriteAsync("pm uninstall com.amazon.example"));
    }

    [Theory]
    [InlineData("pm disable-user --user 0 com.amazon.example")]
    [InlineData("settings put global limit_ad_tracking 1")]
    [InlineData("getprop; pm disable-user --user 0 com.amazon.example")]
    public async Task ShellReadAsync_WithWriteCommand_Throws(string command)
    {
        var adb = new AdbClient("192.0.2.1:5555", applyEnabled: false);

        await Assert.ThrowsAsync<ArgumentException>(() => adb.ShellReadAsync(command));
    }
}
