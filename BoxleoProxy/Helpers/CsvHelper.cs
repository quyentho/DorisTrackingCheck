using System.Text;
using BoxleoProxy.Models;

namespace BoxleoProxy.Helpers;

public static class CsvHelper
{
    /// <summary>
    /// Generates a CSV string from a list of BoxleoOrderDto objects
    /// </summary>
    /// <param name="orders">List of orders to convert to CSV</param>
    /// <returns>CSV formatted string with headers and data rows</returns>
    public static string GenerateCsv(List<BoxleoOrderDto> orders)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("Order ID,Customer name,Customer Number,Address,Products,Price,Status,Delivery date,Comments");

        // Data rows
        foreach (var order in orders)
        {
            csv.AppendLine($"{EscapeCsv(order.OrderId)},{EscapeCsv(order.CustomerName)},{EscapeCsv(order.CustomerNumber)},{EscapeCsv(order.Address)},{EscapeCsv(order.Products)},{order.Price},{EscapeCsv(order.Status)},{EscapeCsv(order.DeliveryDate)},{EscapeCsv(order.Comments)}");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Escapes a string value for safe use in CSV format
    /// </summary>
    /// <param name="value">The string value to escape</param>
    /// <returns>CSV-safe escaped string</returns>
    public static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If value contains comma, newline, or quote, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('\n') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}