using IdeaBoard.Shared.Services.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Custom authentication state provider that uses JWT tokens from Supabase.
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStorage _tokenStorage;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public CustomAuthStateProvider(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Extract claims from JWT
            var claims = new List<Claim>();

            // Add user ID claim (sub)
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            // Add email claim
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
                claims.Add(new Claim(ClaimTypes.Name, email));
            }

            // Add all other claims from JWT
            foreach (var claim in jwtToken.Claims)
            {
                if (claim.Type != "sub" && claim.Type != "email")
                {
                    claims.Add(claim);
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            // Invalid token
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    /// <summary>
    /// Notifies the authentication state has changed (called after login/logout).
    /// </summary>
    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
