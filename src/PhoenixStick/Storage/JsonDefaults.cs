using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixStick.Storage;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // Umlaute lesbar speichern statt als Unicode-Escape
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Enums als Text ("safe" statt 0). Zahlen sind nicht erlaubt, damit ein Tippfehler auffällt
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
}
