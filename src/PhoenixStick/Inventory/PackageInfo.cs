namespace PhoenixStick.Inventory;

public record PackageInfo(string Name, bool IsEnabled, bool IsSystem)
{
    public const string AmazonGroup = "com.amazon.*";
    public const string AndroidGroup = "com.android.*";
    public const string OtherGroup = "Sonstige";

    public string Group =>
        Name.StartsWith("com.amazon.", StringComparison.Ordinal) ? AmazonGroup
        : Name.StartsWith("com.android.", StringComparison.Ordinal) ? AndroidGroup
        : OtherGroup;
}
