using IdeaBoard.Shared.Services.Authentication;
using System.Net;
using System.Net.Http.Headers;

namespace IdeaBoard.Features.Authentication.Services;

/// <summary>
/// HTTP message handler that automatically adds Authorization header with JWT token
/// and handles 401 responses by refreshing the token.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;
    private readonly IServiceProvider _serviceProvider;
    private bool _isRefreshing = false;

    public AuthHeaderHandler(ITokenStorage tokenStorage, IServiceProvider serviceProvider)
    {
        _tokenStorage = tokenStorage;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get access token and add to Authorization header
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // If 401 Unauthorized, try to refresh token and retry
        if (response.StatusCode == HttpStatusCode.Unauthorized && !_isRefreshing)
        {
            _isRefreshing = true;

            try
            {
                // Use a new scope to get IAuthService (avoid circular dependency)
                using var scope = _serviceProvider.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                // Attempt token refresh
                await authService.RefreshTokenAsync();

                // Get the new token
                var newAccessToken = await _tokenStorage.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    // Clone the original request (cannot reuse the same request object)
                    var clonedRequest = await CloneHttpRequestAsync(request);
                    clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                    // Retry the request with new token
                    response = await base.SendAsync(clonedRequest, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Token refresh failed - log error but return 401 response
                // The calling code can handle the error and show inline message
                Console.WriteLine($"Token refresh failed in AuthHeaderHandler: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }

    /// <summary>
    /// Clones an HTTP request message (necessary because requests can only be sent once).
    /// </summary>
    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
