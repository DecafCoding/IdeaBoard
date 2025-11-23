using System.Text.Json.Serialization;

namespace IdeaBoard.Features.Authentication.Models;

/// <summary>
/// Error response from Supabase Auth API.
/// </summary>
public class AuthError
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets a user-friendly error message.
    /// </summary>
    public string GetDisplayMessage()
    {
        return ErrorDescription ?? Message ?? Error ?? "An unknown error occurred.";
    }
}
