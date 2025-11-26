namespace IdeaBoard.Shared.Exceptions.Supabase;

/// <summary>
/// Base exception for Supabase database operations.
/// </summary>
public class SupabaseDbException : Exception
{
    /// <summary>
    /// Gets the HTTP response content if available.
    /// </summary>
    public string? ResponseContent { get; }

    public SupabaseDbException(string message) : base(message)
    {
    }

    public SupabaseDbException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SupabaseDbException(string message, string? responseContent)
        : base(message)
    {
        ResponseContent = responseContent;
    }
}

/// <summary>
/// Exception thrown when a 401 Unauthorized response is received from Supabase.
/// Indicates authentication token is missing, invalid, or expired.
/// </summary>
public class SupabaseUnauthorizedException : SupabaseDbException
{
    public SupabaseUnauthorizedException(string message, string? responseContent)
        : base(message, responseContent)
    {
    }
}

/// <summary>
/// Exception thrown when a 403 Forbidden response is received from Supabase.
/// Typically indicates Row-Level Security (RLS) policy violation.
/// </summary>
public class SupabaseForbiddenException : SupabaseDbException
{
    public SupabaseForbiddenException(string message, string? responseContent)
        : base(message, responseContent)
    {
    }
}

/// <summary>
/// Exception thrown when a 404 Not Found response is received from Supabase.
/// Indicates the requested resource does not exist.
/// </summary>
public class SupabaseNotFoundException : SupabaseDbException
{
    public SupabaseNotFoundException(string message, string? responseContent)
        : base(message, responseContent)
    {
    }
}
