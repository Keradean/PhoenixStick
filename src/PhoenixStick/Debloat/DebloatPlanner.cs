using PhoenixStick.Catalog;
using PhoenixStick.Inventory;

namespace PhoenixStick.Debloat;

public enum SkipReason
{
    NeverTouch,
    ThirdParty,
    NotInCatalog,
    Keep,
    NotInstalled,
    AlreadyDisabled,
    Protected,
    NotInPreset,
    GroupNotIncluded,
}

public record PlannedAction(CatalogEntry Entry, string Command);

public record SkippedPackage(string Package, SkipReason Reason, CatalogEntry? Entry);

public record DebloatPlan(IReadOnlyList<PlannedAction> Actions, IReadOnlyList<SkippedPackage> Skipped);

/// <summary>Entscheidet für jedes Paket, ob es deaktiviert würde. Ändert selbst nichts.</summary>
public static class DebloatPlanner
{
    public static DebloatPlan Plan(
        PackageCatalog catalog,
        IReadOnlyList<PackageInfo> installed,
        Preset preset,
        IReadOnlyCollection<string> includedGroups,
        IReadOnlyCollection<string>? keep = null)
    {
        keep ??= [];
        var actions = new List<PlannedAction>();
        var skipped = new List<SkippedPackage>();

        foreach (var package in installed.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var entry = catalog.Find(package.Name);
            var reason = GetSkipReason(catalog, package, entry, preset, includedGroups, keep);

            if (reason is null)
                actions.Add(new PlannedAction(entry!, PackageCommands.Disable(package.Name)));
            else
                skipped.Add(new SkippedPackage(package.Name, reason.Value, entry));
        }

        var installedNames = installed.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in catalog.Entries.Where(e => !installedNames.Contains(e.Package)))
            skipped.Add(new SkippedPackage(entry.Package, SkipReason.NotInstalled, entry));

        return new DebloatPlan(actions, skipped);
    }

    // Reihenfolge ist wichtig: die harten Schutzregeln zuerst
    private static SkipReason? GetSkipReason(
        PackageCatalog catalog,
        PackageInfo package,
        CatalogEntry? entry,
        Preset preset,
        IReadOnlyCollection<string> includedGroups,
        IReadOnlyCollection<string> keep)
    {
        if (catalog.IsNeverTouch(package.Name))
            return SkipReason.NeverTouch;
        if (!package.IsSystem)
            return SkipReason.ThirdParty;
        if (entry is null)
            return SkipReason.NotInCatalog;
        if (keep.Contains(package.Name))
            return SkipReason.Keep;
        if (!package.IsEnabled)
            return SkipReason.AlreadyDisabled;
        if (entry.Protected)
            return SkipReason.Protected;
        if (!Presets.Includes(preset, entry.Category))
            return SkipReason.NotInPreset;
        if (entry.Group is not null && !includedGroups.Contains(entry.Group, StringComparer.OrdinalIgnoreCase))
            return SkipReason.GroupNotIncluded;
        return null;
    }

    public static string Describe(SkipReason reason) => reason switch
    {
        SkipReason.NeverTouch => "never touch",
        SkipReason.ThirdParty => "Drittanbieter-App, wird nie angefasst",
        SkipReason.NotInCatalog => "nicht im Katalog",
        SkipReason.Keep => "behalten (config.json)",
        SkipReason.NotInstalled => "nicht auf dem Gerät",
        SkipReason.AlreadyDisabled => "schon deaktiviert",
        SkipReason.Protected => "von Fire OS geschützt",
        SkipReason.NotInPreset => "nicht im Preset",
        SkipReason.GroupNotIncluded => "Gruppe nicht mit --include gewählt",
        _ => reason.ToString(),
    };
}
