using Microsoft.JSInterop;

namespace IdeaBoard.Shared.Services.Authentication;

/// <summary>
/// Service for synchronizing authentication state across browser tabs.
/// Listens for localStorage changes and updates auth state when tokens change in other tabs.
/// </summary>
public class AuthSyncService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly CustomAuthStateProvider _authStateProvider;
    private DotNetObjectReference<AuthSyncService>? _dotNetReference;
    private bool _isInitialized = false;

    public AuthSyncService(IJSRuntime jsRuntime, CustomAuthStateProvider authStateProvider)
    {
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Initialize the cross-tab auth synchronization.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _dotNetReference = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("authSync.initialize", _dotNetReference);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize auth sync: {ex.Message}");
        }
    }

    /// <summary>
    /// Called by JavaScript when auth state changes in another tab.
    /// </summary>
    [JSInvokable]
    public void OnAuthStateChangedInOtherTab()
    {
        try
        {
            // Notify the auth state provider to refresh the authentication state
            _authStateProvider.NotifyAuthStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling auth state change from other tab: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isInitialized)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("authSync.dispose");
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        _dotNetReference?.Dispose();
    }
}
