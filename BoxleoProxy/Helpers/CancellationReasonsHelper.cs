using System.Text.Json;

namespace BoxleoProxy.Helpers;

public static class CancellationReasonsHelper
{
    /// <summary>
    /// Parses JSON content containing cancellation reasons and returns a dictionary mapping IDs to reason text
    /// </summary>
    /// <param name="jsonContent">JSON content string containing cancellation reasons array</param>
    /// <returns>Dictionary mapping cancellation reason IDs to their text descriptions</returns>
    public static Dictionary<int, string> ParseCancellationReasons(string jsonContent)
    {
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var reasons = new Dictionary<int, string>();

        foreach (var reason in jsonDoc.RootElement.EnumerateArray())
        {
            if (reason.TryGetProperty("id", out var idProp) &&
                reason.TryGetProperty("reason", out var reasonProp))
            {
                var id = idProp.GetInt32();
                var reasonText = reasonProp.GetString() ?? "";
                reasons[id] = reasonText;
            }
        }

        return reasons;
    }
}