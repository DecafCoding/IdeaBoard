using IdeaBoard.Features.Authentication.Models;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Service for authentication operations with Supabase Auth.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with email and password.
    /// </summary>
    /// <param name="request">Registration request containing email and password.</param>
    /// <returns>Authentication response from Supabase.</returns>
    /// <exception cref="AuthenticationException">Thrown when registration fails.</exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Logs in an existing user with email and password.
    /// </summary>
    /// <param name="request">Login request containing email and password.</param>
    /// <param name="rememberMe">Whether to use extended refresh token lifetime (30 days vs 7 days).</param>
    /// <returns>Authentication response from Supabase.</returns>
    /// <exception cref="AuthenticationException">Thrown when login fails.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, bool rememberMe = false);

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    /// <returns>New authentication response with refreshed tokens.</returns>
    /// <exception cref="AuthenticationException">Thrown when token refresh fails.</exception>
    Task<AuthResponse> RefreshTokenAsync();

    // TODO: Add LogoutAsync for Story A2.3
}
