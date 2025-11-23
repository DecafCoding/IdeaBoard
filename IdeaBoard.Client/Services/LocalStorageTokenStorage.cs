using IdeaBoard.Shared.Services.Authentication;
using Microsoft.JSInterop;
using System.Text;

namespace IdeaBoard.Client.Services;

/// <summary>
/// WebAssembly token storage using browser localStorage (base64 encoded).
/// </summary>
public class LocalStorageTokenStorage : ITokenStorage
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string ExpirationKey = "auth_token_expiration";

    public LocalStorageTokenStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var encoded = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
            return DecodeToken(encoded);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            var encoded = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            return DecodeToken(encoded);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        try
        {
            var encodedAccess = EncodeToken(accessToken);
            var encodedRefresh = EncodeToken(refreshToken);

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, encodedAccess);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, encodedRefresh);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpirationKey, expiresAt.ToString("o"));
        }
        catch
        {
            // Silently fail for MVP
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpirationKey);
        }
        catch
        {
            // Silently fail for MVP
        }
    }

    public async Task<DateTime?> GetTokenExpirationAsync()
    {
        try
        {
            var expirationStr = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ExpirationKey);
            if (!string.IsNullOrEmpty(expirationStr) && DateTime.TryParse(expirationStr, out var expiration))
            {
                return expiration;
            }
        }
        catch
        {
            // Silently fail for MVP
        }

        return null;
    }

    private static string EncodeToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(bytes);
    }

    private static string? DecodeToken(string? encodedToken)
    {
        if (string.IsNullOrEmpty(encodedToken)) return null;

        try
        {
            var bytes = Convert.FromBase64String(encodedToken);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
