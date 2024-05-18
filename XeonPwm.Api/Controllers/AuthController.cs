using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XeonPwm.Api.Contexts;
using XeonPwm.Api.Models.Db;
using XeonPwm.Api.Services;
using XeonPwm.Data.Payloads.ToApi;

namespace XeonPwm.Api.Controllers;

[ApiController]
[Route("/api/auth")]
public class AuthController : ControllerBase
{
    private readonly XeonPwmContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenCache _cache;

    public AuthController(XeonPwmContext context, IPasswordHasher hasher, ITokenCache cache)
    {
        _context = context;
        _hasher = hasher;
        _cache = cache;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !_hasher.Verify(request.Password, user.Hash))
        {
            return Unauthorized();
        }

        var token = new AuthToken()
        {
            UserId = user.Id,
            Token = GenerateToken(),
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            IsForHub = false
        };
        await _context.Tokens.AddAsync(token);
        await _context.SaveChangesAsync();
        await _cache.AddTokenAsync(token);
        
        return Ok(new
        {
            token.Token, 
            token.ExpirationDate
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var usedToken = User.Claims.SingleOrDefault(c => c.Type == "Token")!.Value;
        await _cache.InvalidateTokenAsync(usedToken);
        await _context.Tokens
            .Where(t => t.Token == usedToken)
            .ExecuteDeleteAsync();

        return Ok();
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value.ToString();
        if (userId == null)
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user == null)
        {
            return BadRequest();
        }

        if (!_hasher.Verify(request.OldPassword, user.Hash))
        {
            return BadRequest(new
            {
                Error = "The old password was wrong"
            });
        }

        user.Hash = _hasher.Hash(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [Authorize]
    [HttpPost("hubToken")]
    public async Task<IActionResult> GenerateHubToken()
    {
        var userId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value.ToString();
        if (userId == null)
        {
            return Forbid();
        }
        
        var hubToken = new AuthToken()
        {
            User = new User()
            {
                Id = int.Parse(userId),
                Username = HttpContext.User.Claims.Single(c => c.Type == ClaimTypes.Name).Value.ToString(),
                Hash = string.Empty
            },
            UserId = int.Parse(userId),
            IsForHub = true,
            ExpirationDate = DateTime.UtcNow.AddMinutes(1),
            Token = GenerateToken()
        };
        
        // No need to add hub tokens to DB, they expire after a minute anyway
        await _cache.AddTokenAsync(hubToken);

        return Ok(new
        {
            hubToken.Token
        });
    }

    private string GenerateToken() => RandomNumberGenerator.GetHexString(30);
}