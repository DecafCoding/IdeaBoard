using IdeaBoard.Shared.Services.Authentication;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Security.Claims;

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
    // Using a single global store with user ID as key for simplicity
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

        // For Blazor Server Interactive mode, use the authenticated user's ID as session key
        // This persists across SignalR connections
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user_{userId}";
            }
        }

        // Last resort: use a connection-based identifier or default
        return context?.Connection.Id ?? "default";
    }

    public Task<string?> GetAccessTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;

        // Try cookies first (if HTTP context is available)
        if (context != null && context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token))
        {
            return Task.FromResult<string?>(token);
        }

        // Fallback to in-memory storage using stable session key
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

        // Try cookies first (if HTTP context is available)
        if (context != null && context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token))
        {
            return Task.FromResult<string?>(token);
        }

        // Fallback to in-memory storage using stable session key
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
        var sessionKey = GetSessionKey();

        // Always store in memory for Blazor Server Interactive mode
        _memoryStorage[sessionKey] = new TokenData
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        // Also try to set cookies if context is available and response hasn't started
        if (context != null && !context.Response.HasStarted)
        {
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
        }

        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        var sessionKey = GetSessionKey();

        // Clear in-memory storage
        _memoryStorage.TryRemove(sessionKey, out _);

        // Clear cookies if context is available and response hasn't started
        if (context != null && !context.Response.HasStarted)
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

        // Try cookies first (if HTTP context is available)
        if (context != null && context.Request.Cookies.TryGetValue(ExpirationCookieName, out var expirationStr))
        {
            if (DateTime.TryParse(expirationStr, out var expiration))
            {
                return Task.FromResult<DateTime?>(expiration);
            }
        }

        // Fallback to in-memory storage using stable session key
        var sessionKey = GetSessionKey();
        if (_memoryStorage.TryGetValue(sessionKey, out var data))
        {
            return Task.FromResult(data.ExpiresAt);
        }

        return Task.FromResult<DateTime?>(null);
    }
}
