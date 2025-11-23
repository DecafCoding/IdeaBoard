namespace IdeaBoard.Shared.Services;

/// <summary>
/// Service for managing toast notifications.
/// </summary>
public class NotificationService
{
    private readonly List<Notification> _notifications = new();
    private int _nextId = 1;

    /// <summary>
    /// Event fired when notifications change.
    /// </summary>
    public event Action? NotificationsChanged;

    /// <summary>
    /// Gets all active notifications.
    /// </summary>
    public IReadOnlyList<Notification> Notifications => _notifications.AsReadOnly();

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    public void Success(string message, int durationMs = 5000)
    {
        AddNotification(NotificationType.Success, message, durationMs);
    }

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public void Error(string message, int durationMs = 5000)
    {
        AddNotification(NotificationType.Error, message, durationMs);
    }

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    public void Warning(string message, int durationMs = 5000)
    {
        AddNotification(NotificationType.Warning, message, durationMs);
    }

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    public void Info(string message, int durationMs = 5000)
    {
        AddNotification(NotificationType.Info, message, durationMs);
    }

    /// <summary>
    /// Removes a notification by ID.
    /// </summary>
    public void Remove(int id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _notifications.Remove(notification);
            NotificationsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Adds a new notification to the queue.
    /// </summary>
    private void AddNotification(NotificationType type, string message, int durationMs)
    {
        var notification = new Notification
        {
            Id = _nextId++,
            Type = type,
            Message = message,
            DurationMs = durationMs,
            CreatedAt = DateTime.UtcNow
        };

        _notifications.Add(notification);
        NotificationsChanged?.Invoke();

        // Auto-remove after duration
        if (durationMs > 0)
        {
            _ = Task.Delay(durationMs).ContinueWith(_ => Remove(notification.Id));
        }
    }
}

/// <summary>
/// Represents a notification message.
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}
