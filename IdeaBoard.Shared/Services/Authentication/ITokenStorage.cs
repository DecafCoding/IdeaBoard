namespace IdeaBoard.Shared.Services.Authentication;

/// <summary>
/// Interface for storing and retrieving authentication tokens.
/// Implementations vary by render mode (Server uses cookies, WebAssembly uses localStorage).
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Retrieves the stored access token.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Retrieves the stored refresh token.
    /// </summary>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Stores both access and refresh tokens.
    /// </summary>
    Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt);

    /// <summary>
    /// Clears all stored tokens.
    /// </summary>
    Task ClearTokensAsync();

    /// <summary>
    /// Gets the token expiration time.
    /// </summary>
    Task<DateTime?> GetTokenExpirationAsync();
}
