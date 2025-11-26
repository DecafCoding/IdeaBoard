using IdeaBoard.Shared.Services.Authentication;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Server-side token storage using HTTP-only secure cookies with in-memory fallback.
/// Falls back to in-memory storage when cookies cannot be set (e.g., during Blazor Server SignalR callbacks).
/// </summary>
public class CookieTokenStorage : ITokenStorage
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string AccessTokenCookieName = "auth_access_token";
    private const string RefreshTokenCookieName = "auth_refresh_token";
    private const string ExpirationCookieName = "auth_token_expiration";

    // In-memory fallback storage for Blazor Server Interactive mode
    private static readonly ConcurrentDictionary<string, TokenData> _memoryStorage = new();

    private class TokenData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public CookieTokenStorage(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetSessionKey()
    {
        var context = _httpContextAccessor.HttpContext;
        // Use connection ID or create a session identifier
        // For Blazor Server, we can use the TraceIdentifier as a session key
        return context?.TraceIdentifier ?? "default";
    }

    public Task<string?> GetAccessTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<string?>(null);

        // Try cookies first
        if (context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token))
        {
            return Task.FromResult<string?>(token);
        }

        // Fallback to in-memory storage
        var sessionKey = GetSessionKey();
        if (_memoryStorage.TryGetValue(sessionKey, out var data))
        {
            return Task.FromResult(data.AccessToken);
        }

        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetRefreshTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<string?>(null);

        // Try cookies first
        if (context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token))
        {
            return Task.FromResult<string?>(token);
        }

        // Fallback to in-memory storage
        var sessionKey = GetSessionKey();
        if (_memoryStorage.TryGetValue(sessionKey, out var data))
        {
            return Task.FromResult(data.RefreshToken);
        }

        return Task.FromResult<string?>(null);
    }

    public Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.CompletedTask;

        var sessionKey = GetSessionKey();

        // Check if the response has already started (e.g., in Blazor Server SignalR context)
        // If headers have been sent, we cannot set cookies
        if (context.Response.HasStarted)
        {
            // Fallback to in-memory storage for Blazor Server Interactive mode
            _memoryStorage[sessionKey] = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };
            return Task.CompletedTask;
        }

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

        // Also store in memory as backup
        _memoryStorage[sessionKey] = new TokenData
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.CompletedTask;

        var sessionKey = GetSessionKey();

        // Clear in-memory storage
        _memoryStorage.TryRemove(sessionKey, out _);

        // Clear cookies if response hasn't started
        if (!context.Response.HasStarted)
        {
            context.Response.Cookies.Delete(AccessTokenCookieName);
            context.Response.Cookies.Delete(RefreshTokenCookieName);
            context.Response.Cookies.Delete(ExpirationCookieName);
        }

        return Task.CompletedTask;
    }

    public Task<DateTime?> GetTokenExpirationAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Task.FromResult<DateTime?>(null);

        // Try cookies first
        if (context.Request.Cookies.TryGetValue(ExpirationCookieName, out var expirationStr))
        {
            if (DateTime.TryParse(expirationStr, out var expiration))
            {
                return Task.FromResult<DateTime?>(expiration);
            }
        }

        // Fallback to in-memory storage
        var sessionKey = GetSessionKey();
        if (_memoryStorage.TryGetValue(sessionKey, out var data))
        {
            return Task.FromResult(data.ExpiresAt);
        }

        return Task.FromResult<DateTime?>(null);
    }
}
