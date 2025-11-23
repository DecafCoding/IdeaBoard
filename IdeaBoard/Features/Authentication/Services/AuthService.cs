using IdeaBoard.Features.Authentication.Models;
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

    public AuthService(SupabaseHttpClient supabaseHttpClient)
    {
        _httpClient = supabaseHttpClient.Client;

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

    // TODO: Implement LoginAsync for Story A2.2
    // TODO: Implement LogoutAsync for Story A2.3
}
