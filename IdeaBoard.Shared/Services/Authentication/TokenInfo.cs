namespace IdeaBoard.Shared.Services.Authentication;

/// <summary>
/// Holds authentication tokens from Supabase.
/// </summary>
public class TokenInfo
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
