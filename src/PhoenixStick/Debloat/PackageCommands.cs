namespace PhoenixStick.Debloat;

/// <summary>
/// Die einzigen schreibenden Befehle für Pakete (Sicherheitsregel 3).
/// Dazu kommt als freigegebene Ausnahme die Hintergrundsperre für Drittanbieter-Apps.
/// </summary>
public static class PackageCommands
{
    public static string Disable(string package) => $"pm disable-user --user 0 {package}";

    public static string Enable(string package) => $"pm enable --user 0 {package}";

    public static string RestrictBackground(string package) => $"appops set {package} RUN_IN_BACKGROUND ignore";

    public static string AllowBackground(string package) => $"appops set {package} RUN_IN_BACKGROUND allow";
}
