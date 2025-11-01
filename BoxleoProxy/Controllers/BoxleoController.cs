using Microsoft.AspNetCore.Mvc;
using BoxleoProxy.Services;
using BoxleoProxy.Models;
using System.Text.Json;
using System.Text;

namespace BoxleoProxy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoxleoController : ControllerBase
{
    private readonly IBoxleoAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BoxleoController> _logger;

    public BoxleoController(
        IBoxleoAuthService authService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BoxleoController> logger)
    {
        _authService = authService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 15,
        [FromQuery] string orders_type = "leads",
        [FromQuery] string is_marketplace = "all",
        [FromQuery] string? filter = null)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            var client = _httpClientFactory.CreateClient();

            var url = BuildBoxleoUrl(page, per_page, orders_type, is_marketplace, filter);

            var selectedCountry = new
            {
                id = 20,
                old_id = "5",
                name = "zambia",
                currency = "ZMW",
                currency_exchange_rate = 1,
                flag = "zm",
                phone_code = "+260",
                timezone = "Africa/Lusaka",
                office_address = "Off Fox Dale, Fox Dale Road, Lusaka, Lusaka Province,\nZambia",
                latitude = (string?)null,
                longitude = (string?)null,
                created_at = "2025-07-11T06:01:20.000000Z",
                updated_at = "2025-07-11T06:01:20.000000Z",
                deleted_at = (string?)null,
                terms = "USSD MERCHANT PAYMENT *543*859223*amount# Airtel merchant code payment details *115* 889004130 *amount# For more information, please contact us within 12 hours of receiving the order",
                phone = (string?)null,
                email = "zambia@boxleocourier.ccom",
                about = (string?)null,
                notes = (string?)null
            };

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.7");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("x-selected-country", JsonSerializer.Serialize(selectedCountry));
            request.Headers.Add("x-selected-warehouse", "null");

            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token expired, refreshing and retrying");
                await _authService.RefreshTokenAsync();
                token = await _authService.GetTokenAsync();

                request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json, text/plain, */*");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.7");
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("x-selected-country", JsonSerializer.Serialize(selectedCountry));
                request.Headers.Add("x-selected-warehouse", "null");

                response = await client.SendAsync(request);
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Boxleo orders");
            return StatusCode(500, new { error = "Failed to fetch orders", message = ex.Message });
        }
    }

    [HttpGet("orders/csv")]
    public async Task<IActionResult> GetOrdersCsv(
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 5000,
        [FromQuery] string orders_type = "leads",
        [FromQuery] string is_marketplace = "all",
        [FromQuery] string? filter = null)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            var client = _httpClientFactory.CreateClient();

            var url = BuildBoxleoUrl(page, per_page, orders_type, is_marketplace, filter);

            var selectedCountry = new
            {
                id = 20,
                old_id = "5",
                name = "zambia",
                currency = "ZMW",
                currency_exchange_rate = 1,
                flag = "zm",
                phone_code = "+260",
                timezone = "Africa/Lusaka",
                office_address = "Off Fox Dale, Fox Dale Road, Lusaka, Lusaka Province,\nZambia",
                latitude = (string?)null,
                longitude = (string?)null,
                created_at = "2025-07-11T06:01:20.000000Z",
                updated_at = "2025-07-11T06:01:20.000000Z",
                deleted_at = (string?)null,
                terms = "USSD MERCHANT PAYMENT *543*859223*amount# Airtel merchant code payment details *115* 889004130 *amount# For more information, please contact us within 12 hours of receiving the order",
                phone = (string?)null,
                email = "zambia@boxleocourier.ccom",
                about = (string?)null,
                notes = (string?)null
            };

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.7");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("x-selected-country", JsonSerializer.Serialize(selectedCountry));
            request.Headers.Add("x-selected-warehouse", "null");

            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token expired, refreshing and retrying");
                await _authService.RefreshTokenAsync();
                token = await _authService.GetTokenAsync();

                request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json, text/plain, */*");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.7");
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("x-selected-country", JsonSerializer.Serialize(selectedCountry));
                request.Headers.Add("x-selected-warehouse", "null");

                response = await client.SendAsync(request);
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            var orders = TransformToDto(jsonDoc);
            var csv = GenerateCsv(orders);

            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "boxleo-orders.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Boxleo orders CSV");
            return StatusCode(500, new { error = "Failed to fetch orders CSV", message = ex.Message });
        }
    }

    private List<BoxleoOrderDto> TransformToDto(JsonDocument jsonDoc)
    {
        var orders = new List<BoxleoOrderDto>();

        if (!jsonDoc.RootElement.TryGetProperty("orders", out var ordersArray))
            return orders;

        foreach (var order in ordersArray.EnumerateArray())
        {
            var orderId = GetStringProperty(order, "order_client_id");
            var customerName = GetStringProperty(order, "customer_name");
            var customerPhone = GetStringProperty(order, "customer_phone_1");
            var address = GetStringProperty(order, "customer_address");
            var totalPrice = GetDecimalProperty(order, "total_price");
            var note = GetStringProperty(order, "note");

            // Get status
            var status = GetNestedStringProperty(order, "shipping_status", "display_name");

            // Determine delivery date based on status
            var deliveryDate = GetDeliveryDate(order, status);

            // Process each order item (product)
            if (order.TryGetProperty("order_items", out var orderItems))
            {
                foreach (var item in orderItems.EnumerateArray())
                {
                    var productName = GetNestedStringProperty(item, "product", "name");
                    var quantity = GetIntProperty(item, "quantity");

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
                        Comments = note
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
                    Comments = note
                });
            }
        }

        return orders;
    }

    private string GetDeliveryDate(JsonElement order, string status)
    {
        // Based on status, return the appropriate date
        return status?.ToLower() switch
        {
            "delivered" => GetStringProperty(order, "delivered_at"),
            "in transit" or "shipped" => GetStringProperty(order, "shipped_at"),
            "awaiting dispatch" => GetStringProperty(order, "shipping_date"),
            "scheduled" => GetStringProperty(order, "shipping_date"),
            "pending" => GetStringProperty(order, "pending_since"),
            _ => GetStringProperty(order, "shipping_date") ?? GetStringProperty(order, "shipped_at")
        };
    }

    private string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    private string GetNestedStringProperty(JsonElement element, string parentProperty, string childProperty)
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

    private decimal GetDecimalProperty(JsonElement element, string propertyName)
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

    private int GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return 0;
    }

    private string GenerateCsv(List<BoxleoOrderDto> orders)
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

    private string EscapeCsv(string? value)
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

    private string BuildBoxleoUrl(int page, int per_page, string orders_type, string is_marketplace, string? filter)
    {
        var baseUrl = _configuration["Boxleo:BaseUrl"] ?? "https://boxleo-backend-nml82.ondigitalocean.app";
        var url = $"{baseUrl}/api/orders-paginated?page={page}&per_page={per_page}&orders_type={orders_type}&is_marketplace={is_marketplace}";

        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&filter={Uri.EscapeDataString(filter)}";
        }

        return url;
    }
}
