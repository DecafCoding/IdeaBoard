using IdeaBoard.Shared.Services.Supabase;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdeaBoard.Shared.Services;

/// <summary>
/// Provides Supabase REST API operations with authentication support.
/// Scoped service that manages per-request auth tokens.
/// </summary>
public class SupabaseService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public SupabaseService(SupabaseHttpClient supabaseHttpClient)
    {
        _httpClient = supabaseHttpClient.Client;

        // Configure for Supabase REST API
        _httpClient.BaseAddress = new Uri($"{supabaseHttpClient.SupabaseUrl}/rest/v1/");

        // Add Prefer header for returning data after mutations
        if (!_httpClient.DefaultRequestHeaders.Contains("Prefer"))
        {
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Sets the authentication token for subsequent requests.
    /// TODO: Integrate with IAuthenticationService when available.
    /// </summary>
    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    /// <summary>
    /// Gets all records from a table with optional filtering.
    /// </summary>
    public async Task<List<T>> GetAsync<T>(string table, string? filter = null)
    {
        var url = string.IsNullOrEmpty(filter) ? table : $"{table}?{filter}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase GET failed: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return result ?? new List<T>();
    }

    /// <summary>
    /// Gets a single record by ID.
    /// </summary>
    public async Task<T?> GetByIdAsync<T>(string table, Guid id) where T : class
    {
        var response = await _httpClient.GetAsync($"{table}?id=eq.{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase GET failed: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return result?.FirstOrDefault();
    }

    /// <summary>
    /// Creates a new record in the table.
    /// </summary>
    public async Task<T> PostAsync<T>(string table, T data) where T : class
    {
        var response = await _httpClient.PostAsJsonAsync(table, data, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Supabase POST failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return result?.FirstOrDefault() ?? throw new InvalidOperationException("No data returned from insert");
    }

    /// <summary>
    /// Updates an existing record.
    /// </summary>
    public async Task<T> PatchAsync<T>(string table, Guid id, T data) where T : class
    {
        var response = await _httpClient.PatchAsJsonAsync($"{table}?id=eq.{id}", data, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Supabase PATCH failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return result?.FirstOrDefault() ?? throw new InvalidOperationException("No data returned from update");
    }

    /// <summary>
    /// Deletes a record from the table.
    /// </summary>
    public async Task<bool> DeleteAsync(string table, Guid id)
    {
        var response = await _httpClient.DeleteAsync($"{table}?id=eq.{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase DELETE failed: {response.StatusCode}");
        }

        return true;
    }

    /// <summary>
    /// Upserts (insert or update) multiple records in a single batch operation.
    /// Uses Supabase's bulk upsert with resolution=merge-duplicates.
    /// </summary>
    public async Task<List<T>> UpsertBatchAsync<T>(string table, List<T> data) where T : class
    {
        if (data == null || data.Count == 0)
        {
            return new List<T>();
        }

        // Create request message with upsert header
        var request = new HttpRequestMessage(HttpMethod.Post, table)
        {
            Content = JsonContent.Create(data, options: _jsonOptions)
        };

        // Add upsert preference header (merge duplicates on conflict)
        request.Headers.Add("Prefer", "resolution=merge-duplicates,return=representation");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Supabase UPSERT failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return result ?? new List<T>();
    }
}
