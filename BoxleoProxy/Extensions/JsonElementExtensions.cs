using System.Text.Json;

namespace BoxleoProxy.Extensions;

public static class JsonElementExtensions
{
    /// <summary>
    /// Gets a string property value from a JsonElement, returning empty string if not found or null
    /// </summary>
    public static string GetStringProperty(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Gets a nested string property value from a JsonElement (parent.child), returning empty string if not found or null
    /// </summary>
    public static string GetNestedStringProperty(this JsonElement element, string parentProperty, string childProperty)
    {
        if (element.TryGetProperty(parentProperty, out var parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty(childProperty, out var child) &&
            child.ValueKind != JsonValueKind.Null)
        {
            return child.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets a decimal property value from a JsonElement, returning 0 if not found or cannot be parsed
    /// </summary>
    public static decimal GetDecimalProperty(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Gets an integer property value from a JsonElement, returning 0 if not found or cannot be parsed
    /// </summary>
    public static int GetIntProperty(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return 0;
    }
}