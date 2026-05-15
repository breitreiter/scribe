using System.Text.Json;

namespace Scribe.Utils;

public static class Json
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true
    };

    public static readonly JsonSerializerOptions IndentedCamelCase = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
