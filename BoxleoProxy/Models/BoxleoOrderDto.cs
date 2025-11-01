namespace BoxleoProxy.Models;

public class BoxleoOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Products { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DeliveryDate { get; set; }
    public string? Comments { get; set; }
}
