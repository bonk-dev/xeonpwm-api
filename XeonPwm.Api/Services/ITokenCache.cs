using XeonPwm.Api.Models.Db;

namespace XeonPwm.Api.Services;

public interface ITokenCache
{
    Task AddTokenAsync(AuthToken token);
    Task<AuthToken?> CheckIfValidAsync(string token, bool isForHub);
    Task InvalidateTokenAsync(string token, bool? isForHub = null);
}