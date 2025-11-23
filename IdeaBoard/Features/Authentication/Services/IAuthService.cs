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

    // TODO: Add LoginAsync, LogoutAsync, RefreshTokenAsync for Stories A2.2, A2.3
}
