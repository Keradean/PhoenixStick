using PhoenixStick.Adb;
using PhoenixStick.Catalog;
using PhoenixStick.Debloat;
using PhoenixStick.Inventory;
using static PhoenixStick.Tests.Catalog.TempCatalog;

namespace PhoenixStick.Tests.Catalog;

public class DebloatPlannerTests
{
    private static PackageInfo System(string name, bool enabled = true) => new(name, enabled, IsSystem: true);

    private static PackageCatalog LoadCatalog(string catalogJson, string neverTouchJson = "[]")
    {
        using var dir = new TempCatalog(catalogJson, neverTouchJson);
        return CatalogLoader.Load(dir.Directory);
    }

    private static readonly PackageCatalog _threeCategories = LoadCatalog(List(
        Entry("com.amazon.safe", "safe"),
        Entry("com.amazon.optional", "optional"),
        Entry("com.amazon.risky", "risky")));

    private static readonly PackageInfo[] _threeInstalled =
        [System("com.amazon.safe"), System("com.amazon.optional"), System("com.amazon.risky")];

    private static List<string> Planned(DebloatPlan plan) => plan.Actions.Select(a => a.Entry.Package).ToList();

    private static SkipReason ReasonFor(DebloatPlan plan, string package) =>
        plan.Skipped.Single(s => s.Package == package).Reason;

    [Theory]
    [InlineData(Preset.Safe, new[] { "com.amazon.safe" })]
    [InlineData(Preset.Optional, new[] { "com.amazon.optional", "com.amazon.safe" })]
    [InlineData(Preset.Risky, new[] { "com.amazon.optional", "com.amazon.risky", "com.amazon.safe" })]
    public void Plan_PresetContainsRightCategories(Preset preset, string[] expected)
    {
        var plan = DebloatPlanner.Plan(_threeCategories, _threeInstalled, preset, []);

        Assert.Equal(expected, Planned(plan));
    }

    [Fact]
    public void Plan_NeverTouchWins_EvenIfCatalogSaysSafe()
    {
        var catalog = LoadCatalog(List(Entry("com.amazon.tv.launcher", "safe")), NeverTouch("com.amazon.tv.launcher"));

        var plan = DebloatPlanner.Plan(catalog, [System("com.amazon.tv.launcher")], Preset.Risky, ["alexa"]);

        Assert.Empty(plan.Actions);
        Assert.Equal(SkipReason.NeverTouch, ReasonFor(plan, "com.amazon.tv.launcher"));
    }

    [Fact]
    public void Plan_PackageNotInCatalog_NeverPlanned()
    {
        var plan = DebloatPlanner.Plan(_threeCategories, [.. _threeInstalled, System("com.amazon.unknown")], Preset.Risky, []);

        Assert.DoesNotContain("com.amazon.unknown", Planned(plan));
        Assert.Equal(SkipReason.NotInCatalog, ReasonFor(plan, "com.amazon.unknown"));
    }

    [Fact]
    public void Plan_AlreadyDisabled_Skipped()
    {
        var plan = DebloatPlanner.Plan(_threeCategories, [System("com.amazon.safe", enabled: false)], Preset.Safe, []);

        Assert.Empty(plan.Actions);
        Assert.Equal(SkipReason.AlreadyDisabled, ReasonFor(plan, "com.amazon.safe"));
    }

    [Fact]
    public void Plan_ThirdPartyApp_NeverPlanned_EvenIfInCatalog()
    {
        var catalog = LoadCatalog(List(Entry("com.amazon.spiderpork", "safe")));

        var plan = DebloatPlanner.Plan(catalog, [new PackageInfo("com.amazon.spiderpork", true, IsSystem: false)], Preset.Risky, []);

        Assert.Empty(plan.Actions);
        Assert.Equal(SkipReason.ThirdParty, ReasonFor(plan, "com.amazon.spiderpork"));
    }

    [Fact]
    public void Plan_ProtectedPackage_Skipped()
    {
        var catalog = LoadCatalog(List(Entry("com.amazon.device.metrics", "safe", isProtected: true)));

        var plan = DebloatPlanner.Plan(catalog, [System("com.amazon.device.metrics")], Preset.Safe, []);

        Assert.Equal(SkipReason.Protected, ReasonFor(plan, "com.amazon.device.metrics"));
    }

    [Fact]
    public void Plan_Group_OnlyWithInclude()
    {
        var catalog = LoadCatalog(List(Entry("com.amazon.aca", "optional", group: "alexa")));
        PackageInfo[] installed = [System("com.amazon.aca")];

        var without = DebloatPlanner.Plan(catalog, installed, Preset.Risky, []);
        var with = DebloatPlanner.Plan(catalog, installed, Preset.Optional, ["Alexa"]);

        Assert.Equal(SkipReason.GroupNotIncluded, ReasonFor(without, "com.amazon.aca"));
        Assert.Equal(["com.amazon.aca"], Planned(with));
    }

    [Fact]
    public void Plan_KeepList_SkipsPackage()
    {
        var plan = DebloatPlanner.Plan(_threeCategories, _threeInstalled, Preset.Risky, [], keep: ["com.amazon.optional"]);

        Assert.Equal(["com.amazon.risky", "com.amazon.safe"], Planned(plan));
        Assert.Equal(SkipReason.Keep, ReasonFor(plan, "com.amazon.optional"));
    }

    [Fact]
    public void Plan_NeverTouchStillWinsOverKeep()
    {
        var catalog = LoadCatalog(List(Entry("com.amazon.tv.launcher", "safe")), NeverTouch("com.amazon.tv.launcher"));

        var plan = DebloatPlanner.Plan(catalog, [System("com.amazon.tv.launcher")], Preset.Safe, [], keep: ["com.amazon.tv.launcher"]);

        Assert.Equal(SkipReason.NeverTouch, ReasonFor(plan, "com.amazon.tv.launcher"));
    }

    [Fact]
    public void Plan_CatalogEntryNotOnDevice_ReportedAsNotInstalled()
    {
        var plan = DebloatPlanner.Plan(_threeCategories, [System("com.amazon.safe")], Preset.Safe, []);

        Assert.Equal(SkipReason.NotInstalled, ReasonFor(plan, "com.amazon.risky"));
    }

    [Fact]
    public void Plan_CommandIsDisableUserAndPassesWriteWhitelist()
    {
        var plan = DebloatPlanner.Plan(_threeCategories, _threeInstalled, Preset.Safe, []);

        var command = plan.Actions.Single().Command;
        Assert.Equal("pm disable-user --user 0 com.amazon.safe", command);
        Assert.True(AllowedCommands.IsAllowedWrite(command));
    }
}
