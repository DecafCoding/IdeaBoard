using IdeaBoard.Features.Authentication.Models;
using IdeaBoard.Shared.Services.Authentication;
using IdeaBoard.Shared.Services.Supabase;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Implementation of authentication service using Supabase Auth API.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ITokenStorage _tokenStorage;
    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthService(
        SupabaseHttpClient supabaseHttpClient,
        ITokenStorage tokenStorage,
        CustomAuthStateProvider authStateProvider)
    {
        _httpClient = supabaseHttpClient.Client;
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Registers a new user with email and password.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var requestBody = new
            {
                email = request.Email,
                password = request.Password
            };

            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/signup",
                requestBody,
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                return authResponse ?? throw new AuthenticationException("Invalid response from server.");
            }

            // Handle error responses
            var errorResponse = await response.Content.ReadFromJsonAsync<AuthError>(_jsonOptions);
            var errorMessage = errorResponse?.GetDisplayMessage() ?? "Registration failed.";

            throw new AuthenticationException(errorMessage);
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException("Unable to connect to the authentication server.", ex);
        }
        catch (AuthenticationException)
        {
            throw; // Re-throw AuthenticationException as-is
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("An unexpected error occurred during registration.", ex);
        }
    }

    /// <summary>
    /// Logs in an existing user with email and password.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, bool rememberMe = false)
    {
        try
        {
            var requestBody = new
            {
                email = request.Email,
                password = request.Password
            };

            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/token?grant_type=password",
                requestBody,
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                if (authResponse == null)
                {
                    throw new AuthenticationException("Invalid response from server.");
                }

                // Store tokens
                var expiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
                await _tokenStorage.SetTokensAsync(
                    authResponse.AccessToken ?? string.Empty,
                    authResponse.RefreshToken ?? string.Empty,
                    expiresAt);

                // Notify authentication state changed
                _authStateProvider.NotifyAuthStateChanged();

                return authResponse;
            }

            // Handle error responses
            var errorResponse = await response.Content.ReadFromJsonAsync<AuthError>(_jsonOptions);
            var errorMessage = errorResponse?.GetDisplayMessage() ?? "Login failed.";

            throw new AuthenticationException(errorMessage);
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException("Unable to connect to the authentication server.", ex);
        }
        catch (AuthenticationException)
        {
            throw; // Re-throw AuthenticationException as-is
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("An unexpected error occurred during login.", ex);
        }
    }

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new AuthenticationException("No refresh token available.");
            }

            var requestBody = new
            {
                refresh_token = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/token?grant_type=refresh_token",
                requestBody,
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                if (authResponse == null)
                {
                    throw new AuthenticationException("Invalid response from server.");
                }

                // Store new tokens
                var expiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
                await _tokenStorage.SetTokensAsync(
                    authResponse.AccessToken ?? string.Empty,
                    authResponse.RefreshToken ?? string.Empty,
                    expiresAt);

                // Notify authentication state changed
                _authStateProvider.NotifyAuthStateChanged();

                return authResponse;
            }

            // Handle error responses
            var errorResponse = await response.Content.ReadFromJsonAsync<AuthError>(_jsonOptions);
            var errorMessage = errorResponse?.GetDisplayMessage() ?? "Token refresh failed.";

            throw new AuthenticationException(errorMessage);
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException("Unable to connect to the authentication server.", ex);
        }
        catch (AuthenticationException)
        {
            throw; // Re-throw AuthenticationException as-is
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("An unexpected error occurred during token refresh.", ex);
        }
    }

    // TODO: Implement LogoutAsync for Story A2.3
}
