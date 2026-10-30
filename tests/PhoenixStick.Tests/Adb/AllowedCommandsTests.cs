using PhoenixStick.Adb;

namespace PhoenixStick.Tests.Adb;

public class AllowedCommandsTests
{
    [Theory]
    [InlineData("getprop")]
    [InlineData("getprop ro.product.model")]
    [InlineData("pm list packages")]
    [InlineData("pm list packages -d")]
    [InlineData("settings get global limit_ad_tracking")]
    [InlineData("settings list secure")]
    [InlineData("dumpsys package com.amazon.tv.launcher")]
    public void IsAllowedRead_ReadCommands_True(string command) =>
        Assert.True(AllowedCommands.IsAllowedRead(command));

    [Theory]
    [InlineData("")]
    [InlineData("pm disable-user --user 0 com.amazon.example")]
    [InlineData("pm uninstall com.amazon.example")]
    [InlineData("pm clear com.amazon.example")]
    [InlineData("settings put global limit_ad_tracking 1")]
    [InlineData("getprop && reboot")]
    [InlineData("pm list packages | sh")]
    [InlineData("getpropx")]
    public void IsAllowedRead_OtherCommands_False(string command) =>
        Assert.False(AllowedCommands.IsAllowedRead(command));

    [Theory]
    [InlineData("pm disable-user --user 0 com.amazon.example", true)]
    [InlineData("pm enable --user 0 com.amazon.example", true)]
    [InlineData("settings put global limit_ad_tracking 1", true)]
    [InlineData("pm uninstall com.amazon.example", false)]
    [InlineData("pm disable com.amazon.example", false)]
    [InlineData("pm disable-user --user 0 com.a; reboot", false)]
    [InlineData("pm enable --user 0 ", false)]
    public void IsAllowedWrite(string command, bool expected) =>
        Assert.Equal(expected, AllowedCommands.IsAllowedWrite(command));
}
