namespace BoxleoProxy.Services;

public interface IBoxleoAuthService
{
    Task<string> GetTokenAsync();
    Task RefreshTokenAsync();
}
