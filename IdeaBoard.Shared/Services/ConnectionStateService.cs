namespace IdeaBoard.Shared.Services;

/// <summary>
/// Tracks connection state and unsaved changes for the canvas.
/// Used to display connection status banners to the user.
/// </summary>
public class ConnectionStateService
{
    private bool _isOnline = true;
    private bool _hasUnsavedChanges = false;

    /// <summary>
    /// Event fired when connection state changes.
    /// </summary>
    public event Action<bool>? ConnectionStateChanged;

    /// <summary>
    /// Event fired when unsaved changes state changes.
    /// </summary>
    public event Action<bool>? UnsavedChangesStateChanged;

    /// <summary>
    /// Gets whether the application is currently online.
    /// </summary>
    public bool IsOnline => _isOnline;

    /// <summary>
    /// Gets whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    /// <summary>
    /// Sets the connection state.
    /// </summary>
    public void SetConnectionState(bool isOnline)
    {
        if (_isOnline != isOnline)
        {
            _isOnline = isOnline;
            ConnectionStateChanged?.Invoke(_isOnline);
        }
    }

    /// <summary>
    /// Sets the unsaved changes state.
    /// </summary>
    public void SetHasUnsavedChanges(bool hasUnsavedChanges)
    {
        if (_hasUnsavedChanges != hasUnsavedChanges)
        {
            _hasUnsavedChanges = hasUnsavedChanges;
            UnsavedChangesStateChanged?.Invoke(_hasUnsavedChanges);
        }
    }
}
