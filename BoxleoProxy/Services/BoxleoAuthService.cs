using System.Text.Json;

namespace BoxleoProxy.Services;

public class BoxleoAuthService : IBoxleoAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BoxleoAuthService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _cachedToken;

    public BoxleoAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BoxleoAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync()
    {
        // Return cached token if available, otherwise refresh
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            return _cachedToken;
        }

        await RefreshTokenAsync();
        return _cachedToken ?? throw new InvalidOperationException("Failed to obtain token");
    }

    public async Task RefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            // Double-check locking
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                return;
            }

            _logger.LogInformation("Refreshing Boxleo authentication token");

            var client = _httpClientFactory.CreateClient();
            var loginUrl = _configuration["Boxleo:LoginUrl"] ?? throw new InvalidOperationException("Boxleo login URL is not configured");

            var loginData = new
            {
                email = _configuration["Boxleo:Email"],
                password = _configuration["Boxleo:Password"]
            };

            var response = await client.PostAsJsonAsync(loginUrl, loginData);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Extract token from response
            if (responseData.TryGetProperty("token", out var tokenElement))
            {
                _cachedToken = tokenElement.GetString();
            }
            else
            {
                throw new Exception("Token not found in login response");
            }

            _logger.LogInformation("Successfully refreshed Boxleo token");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
