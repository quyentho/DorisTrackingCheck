using System.Text.Json;
using System.Text;
using BoxleoProxy.Models;
using BoxleoProxy.Extensions;

namespace BoxleoProxy.Helpers;

public static class OrderTransformHelper
{
    /// <summary>
    /// Transforms JSON content string containing orders data into a list of BoxleoOrderDto objects
    /// </summary>
    /// <param name="jsonContent">The JSON content string containing orders data</param>
    /// <param name="cancellationReasons">Dictionary mapping cancellation reason IDs to their text descriptions</param>
    /// <returns>List of transformed BoxleoOrderDto objects</returns>
    public static List<BoxleoOrderDto> TransformToDto(string jsonContent, Dictionary<int, string> cancellationReasons)
    {
        var orders = new List<BoxleoOrderDto>();

        using var jsonDoc = JsonDocument.Parse(jsonContent);

        if (!jsonDoc.RootElement.TryGetProperty("orders", out var ordersArray))
            return orders;

        foreach (var order in ordersArray.EnumerateArray())
        {
            var orderId = order.GetStringProperty("order_client_id");
            var customerName = order.GetStringProperty("customer_name");
            var customerPhone = order.GetStringProperty("customer_phone_1");
            var address = order.GetStringProperty("customer_address");
            var totalPrice = order.GetDecimalProperty("total_price");
            var note = order.GetStringProperty("note");

            // Get status - check confirmation status first, then shipping status if confirmed
            var confirmationStatus = order.GetNestedStringProperty("confirmation_status", "display_name");
            var status = confirmationStatus.ToLower() == "confirmed"
                ? order.GetNestedStringProperty("shipping_status", "display_name")
                : confirmationStatus;

            // Determine delivery date based on status
            var deliveryDate = GetDeliveryDate(order, status);

            // Build enhanced note for cancelled orders
            var enhancedNote = BuildEnhancedNote(order, status, note, cancellationReasons);

            // Process each order item (product)
            if (order.TryGetProperty("order_items", out var orderItems))
            {
                foreach (var item in orderItems.EnumerateArray())
                {
                    var productName = item.GetNestedStringProperty("product", "name");
                    var quantity = item.GetIntProperty("quantity");

                    var productDisplay = quantity > 1 ? $"{productName} (x{quantity})" : productName;

                    orders.Add(new BoxleoOrderDto
                    {
                        OrderId = orderId,
                        CustomerName = customerName,
                        CustomerNumber = customerPhone,
                        Address = address,
                        Products = productDisplay,
                        Price = totalPrice,
                        Status = status,
                        DeliveryDate = deliveryDate,
                        Comments = enhancedNote
                    });
                }
            }
            else
            {
                // If no order items, still create a record
                orders.Add(new BoxleoOrderDto
                {
                    OrderId = orderId,
                    CustomerName = customerName,
                    CustomerNumber = customerPhone,
                    Address = address,
                    Products = "",
                    Price = totalPrice,
                    Status = status,
                    DeliveryDate = deliveryDate,
                    Comments = enhancedNote
                });
            }
        }

        return orders;
    }

    /// <summary>
    /// Builds an enhanced note for cancelled orders, including cancellation reason and date
    /// </summary>
    /// <param name="order">The order JSON element</param>
    /// <param name="status">The current order status</param>
    /// <param name="originalNote">The original note from the order</param>
    /// <param name="cancellationReasons">Dictionary mapping cancellation reason IDs to their text descriptions</param>
    /// <returns>Enhanced note string with cancellation details for cancelled orders, or original note for others</returns>
    public static string BuildEnhancedNote(JsonElement order, string status, string originalNote, Dictionary<int, string> cancellationReasons)
    {
        if (status.ToLower() != "cancelled")
        {
            return originalNote;
        }

        var noteBuilder = new StringBuilder();

        // Add cancellation reason
        if (order.TryGetProperty("cancellation_reason_id", out var reasonIdProp) &&
            reasonIdProp.ValueKind == JsonValueKind.Number)
        {
            var reasonId = reasonIdProp.GetInt32();
            var reasonText = cancellationReasons.TryGetValue(reasonId, out var reason) ? reason : "Unknown";

            if (noteBuilder.Length > 0)
                noteBuilder.Append(" | ");

            noteBuilder.Append($"Reason: ({reasonText})");
        }

        // Add cancellation date
        var cancelledAt = order.GetStringProperty("cancelled_at");
        if (!string.IsNullOrEmpty(cancelledAt))
        {
            if (noteBuilder.Length > 0)
                noteBuilder.Append(". ");

            noteBuilder.Append($"Cancelled at: ({cancelledAt})");
        }

        return noteBuilder.ToString();
    }

    /// <summary>
    /// Gets the appropriate delivery date based on the order status
    /// </summary>
    /// <param name="order">The order JSON element</param>
    /// <param name="status">The current order status</param>
    /// <returns>The delivery date string based on the status</returns>
    private static string GetDeliveryDate(JsonElement order, string status)
    {
        // Based on status, return the appropriate date
        return status?.ToLower() switch
        {
            "delivered" => order.GetStringProperty("delivered_at"),
            "in transit" or "shipped" => order.GetStringProperty("shipped_at"),
            "awaiting dispatch" => order.GetStringProperty("shipping_date"),
            "scheduled" => order.GetStringProperty("shipping_date"),
            "pending" => order.GetStringProperty("pending_since"),
            _ => order.GetStringProperty("shipping_date") ?? order.GetStringProperty("shipped_at")
        };
    }
}