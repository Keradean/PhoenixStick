namespace PhoenixStick.Catalog;

/// <summary>Kategorie eines Pakets im Katalog. In JSON klein geschrieben: "safe", "optional", "risky".</summary>
public enum Category
{
    Safe,
    Optional,
    Risky,
}

/// <summary>Auswahl für debloat --preset. Jedes Preset schließt die Kategorien der vorigen ein.</summary>
public enum Preset
{
    Safe,
    Optional,
    Risky,
}

public static class Presets
{
    public static bool Includes(Preset preset, Category category) => category switch
    {
        Category.Safe => true,
        Category.Optional => preset is Preset.Optional or Preset.Risky,
        Category.Risky => preset == Preset.Risky,
        _ => false,
    };
}
