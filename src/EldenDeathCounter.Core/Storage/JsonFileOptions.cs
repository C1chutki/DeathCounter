using System.Text.Json;

namespace EldenDeathCounter.Core.Storage;

public static class JsonFileOptions
{
    public static readonly JsonSerializerOptions Value = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
