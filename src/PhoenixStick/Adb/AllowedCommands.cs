using System.Text.RegularExpressions;

namespace PhoenixStick.Adb;

/// <summary>Whitelists für Shell-Befehle. Alles, was nicht passt, wird abgelehnt.</summary>
public static partial class AllowedCommands
{
    // Präfixe enden mit Leerzeichen, damit z. B. "getpropx" nicht als "getprop" durchgeht
    private static readonly string[] _readPrefixes =
    [
        "getprop ",
        "pm list packages ",
        "pm path ",
        "settings get ",
        "settings list ",
        "dumpsys ",
    ];

    // Lesebefehle, die auch ohne Argumente laufen dürfen
    private static readonly string[] _readExact = ["getprop", "pm list packages"];

    private static readonly string[] _writePrefixes =
    [
        "pm disable-user --user 0 ",
        "pm enable --user 0 ",
        "settings put ",
    ];

    // appops nur für genau diese eine Operation, Paketname und Modus streng geprüft
    [GeneratedRegex(@"^appops set [A-Za-z0-9_.]+ RUN_IN_BACKGROUND (ignore|allow)$")]
    private static partial Regex BackgroundCommand();

    // Verhindert, dass an einen erlaubten Befehl ein zweiter angehängt wird
    private static readonly char[] _shellMetaChars = [';', '&', '|', '>', '<', '`', '$', '\n', '\r'];

    public static bool IsAllowedRead(string command) =>
        _readExact.Contains(command) || IsAllowed(command, _readPrefixes);

    public static bool IsAllowedWrite(string command) =>
        IsAllowed(command, _writePrefixes) || BackgroundCommand().IsMatch(command);

    private static bool IsAllowed(string command, string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(command) || command.IndexOfAny(_shellMetaChars) >= 0)
            return false;

        return prefixes.Any(p => command.StartsWith(p, StringComparison.Ordinal) && command.Length > p.Length);
    }
}
