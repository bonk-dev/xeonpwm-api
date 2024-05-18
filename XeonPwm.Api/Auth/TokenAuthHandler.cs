using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using XeonPwm.Api.Contexts;
using XeonPwm.Api.Models.Db;
using XeonPwm.Api.Services;

namespace XeonPwm.Api.Auth;

public class TokenAuthHandler : AuthenticationHandler<TokenAuthSchemeOptions>
{
    private readonly XeonPwmContext _context;
    private readonly ITokenCache _tokenCache;

    public TokenAuthHandler(IOptionsMonitor<TokenAuthSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        XeonPwmContext context, ITokenCache tokenCache) 
        : base(options, logger, encoder)
    {
        _context = context;
        _tokenCache = tokenCache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var isHubAuth = Request.Path.StartsWithSegments("/hubs/pwm");
        var isHubNegotation = Request.Path.StartsWithSegments("/hubs/pwm/negotiate");
        
        Logger.LogDebug("Is hub: {IsHub}", isHubAuth);
        Logger.LogDebug("URL: {Url}", Request.Path.ToString());
        
        string receivedToken;

        if (isHubAuth && !isHubNegotation)
        {
            if (string.IsNullOrEmpty(Request.Query["access_token"]))
            {
                return AuthenticateResult.NoResult();   
            }

            receivedToken = "Bearer " + Request.Query["access_token"];
        }
        else
        {
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var headerStrValues))
            {
                return AuthenticateResult.NoResult();
            }

            receivedToken = headerStrValues.ToString();
        }
        
        var split = receivedToken.Split(' ');
        if (split.Length < 2)
        {
            return AuthenticateResult.Fail("Invalid token format");
        }
    
        receivedToken = split[1];

        var token = await _tokenCache.CheckIfValidAsync(receivedToken, isHubAuth);
        if (token == null)
        {
            token = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == receivedToken);
            if (token == null)
            {
                return AuthenticateResult.Fail("Invalid token");
            }

            if (token.ExpirationDate < DateTime.UtcNow)
            {
                _context.Tokens.Remove(token);
                await _context.SaveChangesAsync();
                return AuthenticateResult.Fail("Token expired");
            }
        }
        else
        {
            Logger.LogDebug("Cache hit for token");
        }
        
        if (isHubAuth && !isHubNegotation)
        {
            Logger.LogDebug("INVALIDATING");
            await _tokenCache.InvalidateTokenAsync(receivedToken);
        }

        var claims = new Claim[]
        {
            new(ClaimTypes.Name, token.User.Username),
            new(ClaimTypes.NameIdentifier, token.UserId.ToString()),
            new("Token", token.Token)
        };
        var identity = new ClaimsIdentity(claims, "Token");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}