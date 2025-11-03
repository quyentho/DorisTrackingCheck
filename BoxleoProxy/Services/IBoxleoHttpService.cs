using BoxleoProxy.Models;

namespace BoxleoProxy.Services;

public interface IBoxleoHttpService
{
    Task<string> GetOrdersAsync(int page, int per_page, string orders_type, string is_marketplace, string? filter = null);
    Task<Dictionary<int, string>> GetCancellationReasonsAsync();
}