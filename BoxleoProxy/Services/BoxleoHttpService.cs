using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace BoxleoProxy.Services;

public class BoxleoHttpService : IBoxleoHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BoxleoHttpService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static string? _cachedToken;
    private static Task? _ongoingRefreshTask;
    private static int _tokenVersion = 0;

    public BoxleoHttpService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BoxleoHttpService> logger,
        IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetOrdersAsync(int page, int per_page, string orders_type, string is_marketplace, string? filter = null)
    {
        var token = await GetTokenAsync();
        var url = BuildOrdersUrl(page, per_page, orders_type, is_marketplace, filter);

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

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HandleTokenExpired(request);

            response = await _httpClient.SendAsync(request);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<Dictionary<int, string>> GetCancellationReasonsAsync()
    {
        const string cacheKey = "cancellation_reasons";

        if (_memoryCache.TryGetValue(cacheKey, out Dictionary<int, string>? cached))
        {
            return cached ?? new Dictionary<int, string>();
        }

        try
        {
            var token = await GetTokenAsync();
            var url = "/api/cancellation-reasons";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleTokenExpired(request);
                response = await _httpClient.SendAsync(request);
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

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

            // Cache for 1 day
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            };
            _memoryCache.Set(cacheKey, reasons, cacheOptions);

            return reasons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cancellation reasons");
            return new Dictionary<int, string>();
        }
    }

    private async Task HandleTokenExpired(HttpRequestMessage request)
    {
        _logger.LogWarning("Token expired, refreshing and retrying");
        await RefreshTokenAsync();
        var token = await GetTokenAsync();
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"Bearer {token}");
    }

    private string BuildOrdersUrl(int page, int per_page, string orders_type, string is_marketplace, string? filter)
    {
        var url = $"/api/orders-paginated?page={page}&per_page={per_page}&orders_type={orders_type}&is_marketplace={is_marketplace}";

        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&filter={Uri.EscapeDataString(filter)}";
        }

        return url;
    }

    private async Task<string> GetTokenAsync()
    {
        // Return cached token if available, otherwise refresh
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            return _cachedToken;
        }

        await PerformRefreshTokenAsync();
        return _cachedToken ?? throw new InvalidOperationException("Failed to obtain token");
    }

    private async Task RefreshTokenAsync()
    {
        var tempRefreshTask = _ongoingRefreshTask;
        if (tempRefreshTask != null && !tempRefreshTask.IsCompleted)
        {
            // Problem: Between checking "!= null" and ".IsCompleted",
            // another thread could set _ongoingRefreshTask = null
            // causing NullReferenceException on IsCompleted check
            // so we cache the reference first
            return;
        }

        AsyncLocal<int> currentTokenVersion = new();
        currentTokenVersion.Value = _tokenVersion;

        await _refreshLock.WaitAsync();
        try
        {
            if (currentTokenVersion.Value != _tokenVersion)
            {
                // Another thread already refreshed the token
                return;
            }

            _ongoingRefreshTask = PerformRefreshTokenAsync();
            await _ongoingRefreshTask;
            _tokenVersion++;
        }
        finally
        {
            _ongoingRefreshTask = null;
            _refreshLock.Release();
        }
    }

    private async Task PerformRefreshTokenAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing Boxleo authentication token");

            var email = _configuration["Boxleo:Email"] ?? throw new InvalidOperationException("Boxleo email is not configured");
            var password = _configuration["Boxleo:Password"] ?? throw new InvalidOperationException("Boxleo password is not configured");

            _cachedToken = await LoginAsync(email, password);

            _logger.LogInformation("Successfully refreshed Boxleo token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Boxleo authentication token");
            throw;
        }
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        try
        {
            var loginData = new
            {
                email = email,
                password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/login", loginData);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Extract token from response
            if (responseData.TryGetProperty("token", out var tokenElement))
            {
                return tokenElement.GetString() ?? throw new Exception("Token value is null");
            }
            else
            {
                throw new Exception("Token not found in login response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }
}