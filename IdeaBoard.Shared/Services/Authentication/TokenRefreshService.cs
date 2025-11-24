namespace IdeaBoard.Shared.Services.Authentication;

/// <summary>
/// Background service that proactively refreshes access tokens before they expire.
/// </summary>
public class TokenRefreshService : IDisposable
{
    private readonly IAuthService _authService;
    private readonly ITokenStorage _tokenStorage;
    private readonly Timer _timer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
    private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(10); // Refresh if less than 10 minutes remaining

    public TokenRefreshService(IAuthService authService, ITokenStorage tokenStorage)
    {
        _authService = authService;
        _tokenStorage = tokenStorage;
        _timer = new Timer(CheckAndRefreshToken, null, _checkInterval, _checkInterval);
    }

    private async void CheckAndRefreshToken(object? state)
    {
        try
        {
            var expiration = await _tokenStorage.GetTokenExpirationAsync();
            if (expiration == null) return;

            var timeUntilExpiry = expiration.Value - DateTime.UtcNow;

            // If token expires in less than threshold, refresh it
            if (timeUntilExpiry < _refreshThreshold)
            {
                await _authService.RefreshTokenAsync();
            }
        }
        catch
        {
            // Silently fail - user will be prompted to login on next request if token is invalid
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
