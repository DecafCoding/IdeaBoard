using IdeaBoard.Shared.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// Custom authentication handler that validates JWT tokens stored in cookies.
/// Integrates the custom token storage with ASP.NET Core authentication.
/// </summary>
public class JwtCookieAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenStorage _tokenStorage;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtCookieAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITokenStorage tokenStorage)
        : base(options, logger, encoder)
    {
        _tokenStorage = tokenStorage;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Get the access token from cookie storage
            var token = await _tokenStorage.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.NoResult();
            }

            // Validate and parse the JWT token
            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return AuthenticateResult.Fail("Token has expired");
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

            // Create the identity and principal
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating request");
            return AuthenticateResult.Fail("Invalid token");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Redirect to login page when authentication is required
        Response.Redirect("/login");
        return Task.CompletedTask;
    }
}
