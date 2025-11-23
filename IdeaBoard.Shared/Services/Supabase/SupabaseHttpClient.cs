using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace IdeaBoard.Shared.Services.Supabase;

/// <summary>
/// Base HTTP client configured for Supabase API operations.
/// Provides foundational HTTP configuration shared across all Supabase services.
/// </summary>
public class SupabaseHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;

    public SupabaseHttpClient(HttpClient httpClient, IConfiguration config)
    {
        // Validate configuration
        _supabaseUrl = config["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Supabase URL not configured. Add 'Supabase:Url' to appsettings.json or user secrets.");

        _supabaseAnonKey = config["Supabase:AnonKey"]
            ?? throw new InvalidOperationException(
                "Supabase Anon Key not configured. Add 'Supabase:AnonKey' to appsettings.json or user secrets.");

        // Configure HTTP client
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_supabaseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseAnonKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Set timeout (recommended for external API calls)
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the configured HttpClient for Supabase operations.
    /// </summary>
    public HttpClient Client => _httpClient;

    /// <summary>
    /// Gets the Supabase project URL.
    /// </summary>
    public string SupabaseUrl => _supabaseUrl;

    /// <summary>
    /// Gets the Supabase anonymous API key.
    /// </summary>
    public string AnonKey => _supabaseAnonKey;
}
