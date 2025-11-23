using IdeaBoard.Shared.Services.Authentication;
using Microsoft.AspNetCore.Http;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Server-side token storage using HTTP-only secure cookies.
/// </summary>
public class CookieTokenStorage : ITokenStorage
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string AccessTokenCookieName = "auth_access_token";
    private const string RefreshTokenCookieName = "auth_refresh_token";
    private const string ExpirationCookieName = "auth_token_expiration";

    public CookieTokenStorage(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetAccessTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<string?>(null);

        context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token);
        return Task.FromResult(token);
    }

    public Task<string?> GetRefreshTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<string?>(null);

        context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token);
        return Task.FromResult(token);
    }

    public Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.CompletedTask;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30) // Max cookie lifetime
        };

        context.Response.Cookies.Append(AccessTokenCookieName, accessToken, cookieOptions);
        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
        context.Response.Cookies.Append(ExpirationCookieName, expiresAt.ToString("o"), cookieOptions);

        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.CompletedTask;

        context.Response.Cookies.Delete(AccessTokenCookieName);
        context.Response.Cookies.Delete(RefreshTokenCookieName);
        context.Response.Cookies.Delete(ExpirationCookieName);

        return Task.CompletedTask;
    }

    public Task<DateTime?> GetTokenExpirationAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<DateTime?>(null);

        if (context.Request.Cookies.TryGetValue(ExpirationCookieName, out var expirationStr))
        {
            if (DateTime.TryParse(expirationStr, out var expiration))
            {
                return Task.FromResult<DateTime?>(expiration);
            }
        }

        return Task.FromResult<DateTime?>(null);
    }
}
