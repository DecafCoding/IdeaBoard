namespace IdeaBoard.Features.Authentication.Models;

/// <summary>
/// Exception thrown when authentication operations fail.
/// </summary>
public class AuthenticationException : Exception
{
    public string? ErrorCode { get; }

    public AuthenticationException(string message) : base(message)
    {
    }

    public AuthenticationException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public AuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
