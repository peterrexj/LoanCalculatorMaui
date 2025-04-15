using System.Text.Json;

namespace LoanCalculator.Core.Exts;

public static class JsonExts
{
    public static T? DeepCloneObject<T>(this T obj)
    {
        // Serialize the object to JSON and then deserialize it back to create a deep copy
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve // Optional: Handle circular references
        };

        var json = JsonSerializer.Serialize(obj, options);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}
