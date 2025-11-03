using Microsoft.AspNetCore.Mvc;
using BoxleoProxy.Services;
using BoxleoProxy.Models;
using BoxleoProxy.Extensions;
using BoxleoProxy.Helpers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace BoxleoProxy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoxleoController : ControllerBase
{
    private readonly IBoxleoHttpService _boxleoHttpService;
    private readonly ILogger<BoxleoController> _logger;

    public BoxleoController(
        IBoxleoHttpService boxleoHttpService,
        ILogger<BoxleoController> logger)
    {
        _boxleoHttpService = boxleoHttpService;
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
            var content = await _boxleoHttpService.GetOrdersAsync(page, per_page, orders_type, is_marketplace, filter);
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
            var content = await _boxleoHttpService.GetOrdersAsync(page, per_page, orders_type, is_marketplace, filter);

            // Fetch cancellation reasons and cache them
            var cancellationReasons = await _boxleoHttpService.GetCancellationReasonsAsync();

            var orders = OrderTransformHelper.TransformToDto(content, cancellationReasons);
            var csv = CsvHelper.GenerateCsv(orders);

            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "boxleo-orders.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Boxleo orders CSV");
            return StatusCode(500, new { error = "Failed to fetch orders CSV", message = ex.Message });
        }
    }
}
