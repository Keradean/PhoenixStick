namespace PhoenixStick.Tests.Catalog;

/// <summary>Legt Katalogdateien in einem temporären Ordner an und räumt danach auf.</summary>
public sealed class TempCatalog : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "phoenixstick-catalog", Guid.NewGuid().ToString());

    public TempCatalog(string catalogJson, string neverTouchJson = "[]")
    {
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(Path.Combine(Directory, "fireos.json"), catalogJson);
        File.WriteAllText(Path.Combine(Directory, "never-touch.json"), neverTouchJson);
    }

    public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);

    public static string Entry(string package, string category, string? group = null, bool isProtected = false) =>
        $$"""
        { "package": "{{package}}", "label": "{{package}}", "category": "{{category}}",
          {{(group is null ? "" : $"\"group\": \"{group}\",")}}
          "reason": "Test", "breaks": "", "source": "Test", "protected": {{(isProtected ? "true" : "false")}} }
        """;

    public static string List(params string[] entries) => "[" + string.Join(",", entries) + "]";

    public static string NeverTouch(params string[] packages) =>
        List(packages.Select(p => $$"""{ "package": "{{p}}", "reason": "Test", "source": "Test" }""").ToArray());
}
