namespace PhoenixStick.Debloat;

/// <summary>Die einzigen Paketbefehle, die das Tool je schreibend absetzt (Sicherheitsregel 3).</summary>
public static class PackageCommands
{
    public static string Disable(string package) => $"pm disable-user --user 0 {package}";

    public static string Enable(string package) => $"pm enable --user 0 {package}";
}
