using XeonPwm.Api.Models.Db;

namespace XeonPwm.Api.Services.Impl;

public class InMemoryTokenCache : ITokenCache
{
    private readonly Dictionary<string, AuthToken> _validTokens = new();

    public Task AddTokenAsync(AuthToken token)
    {
        var copiedToken = new AuthToken()
        {
            ExpirationDate = token.ExpirationDate,
            Token = token.Token,
            IsForHub = token.IsForHub,
            Id = token.Id,
            UserId = token.UserId,
            User = token.User
        };
        
        _validTokens.Add(token.Token, copiedToken);
        return Task.CompletedTask;
    }

    public async Task<AuthToken?> CheckIfValidAsync(string token, bool isForHub)
    {
        if (_validTokens.TryGetValue(token, out var value))
        {
            return value.IsForHub == isForHub && value.ExpirationDate > DateTime.UtcNow  
                ? value
                : null;
        }

        return null;
    }

    public Task InvalidateTokenAsync(string token, bool? isForHub)
    {
        if (!isForHub.HasValue || 
            (_validTokens.TryGetValue(token, out var value) 
                && value.IsForHub == isForHub.Value))
        {
            _validTokens.Remove(token);
        }
        
        return Task.CompletedTask;
    }
}