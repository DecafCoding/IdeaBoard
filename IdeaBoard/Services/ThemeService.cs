using Microsoft.JSInterop;
using IdeaBoard.Services.Interfaces;

namespace IdeaBoard.Services
{
    /// <summary>
    /// Service for managing application theme (light/dark mode).
    /// Uses localStorage for persistence and Bootstrap's data-bs-theme attribute.
    /// Handles pre-rendering by checking JavaScript availability.
    /// </summary>
    public class ThemeService : IThemeService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "auto"; // Cache for pre-rendering

        public event Action<string>? ThemeChanged;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Gets the current theme from localStorage or defaults to 'auto'.
        /// Returns cached value during pre-rendering.
        /// </summary>
        public async Task<string> GetThemeAsync()
        {
            // Check if JavaScript is available (not during pre-rendering)
            if (!IsJavaScriptAvailable())
            {
                return _currentTheme;
            }

            try
            {
                var theme = await _jsRuntime.InvokeAsync<string>("IdeaBoardTheme.getTheme");
                _currentTheme = theme; // Update cache
                return theme;
            }
            catch
            {
                // Fallback to cached value if JS fails
                return _currentTheme;
            }
        }

        /// <summary>
        /// Sets the theme and persists it to localStorage.
        /// Valid themes: 'light', 'dark', 'auto'
        /// Updates cache immediately for pre-rendering scenarios.
        /// </summary>
        public async Task SetThemeAsync(string theme)
        {
            // Validate and cache theme immediately
            if (theme != "light" && theme != "dark" && theme != "auto")
                theme = "auto";

            _currentTheme = theme;

            // Skip JavaScript during pre-rendering
            if (!IsJavaScriptAvailable())
            {
                ThemeChanged?.Invoke(theme);
                return;
            }

            try
            {
                await _jsRuntime.InvokeVoidAsync("IdeaBoardTheme.setTheme", theme);
                ThemeChanged?.Invoke(theme);
            }
            catch
            {
                // Still notify of change even if persistence fails
                ThemeChanged?.Invoke(theme);
            }
        }

        /// <summary>
        /// Checks if JavaScript interop is available (not during pre-rendering).
        /// </summary>
        private bool IsJavaScriptAvailable()
        {
            try
            {
                // More reliable check for JavaScript availability
                return _jsRuntime is IJSInProcessRuntime ||
                       !_jsRuntime.GetType().Name.Contains("Unsupported");
            }
            catch
            {
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // No module to dispose anymore
            await Task.CompletedTask;
        }
    }
}
