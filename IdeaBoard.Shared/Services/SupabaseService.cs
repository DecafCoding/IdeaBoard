using IdeaBoard.Shared.Exceptions.Supabase;
using IdeaBoard.Shared.Services.Supabase;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdeaBoard.Shared.Services;

/// <summary>
/// Provides Supabase REST API operations with authentication support.
/// Scoped service that manages per-request database operations.
/// Authentication tokens are automatically injected by AuthHeaderHandler.
/// </summary>
public class SupabaseService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupabaseService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseService(
        SupabaseHttpClient supabaseHttpClient,
        ILogger<SupabaseService> logger)
    {
        _httpClient = supabaseHttpClient.Client;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region GET Operations

    /// <summary>
    /// Gets multiple records from a table with optional filtering, ordering, and pagination.
    /// </summary>
    /// <typeparam name="T">The entity type to deserialize to.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="filters">Optional filters using PostgREST operators (e.g., "id" = "eq.123", "age" = "gte.18").</param>
    /// <param name="orderBy">Optional ordering (e.g., "created_at.desc" or "name.asc").</param>
    /// <param name="limit">Optional maximum number of records to return.</param>
    /// <param name="offset">Optional number of records to skip.</param>
    /// <returns>List of records matching the criteria.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<List<T>> GetAsync<T>(
        string table,
        Dictionary<string, string>? filters = null,
        string? orderBy = null,
        int? limit = null,
        int? offset = null)
    {
        try
        {
            var queryString = BuildQueryString(filters, orderBy, limit, offset);
            var url = $"rest/v1/{table}{queryString}";

            _logger.LogDebug("GET request to {Table}: {QueryString}", table, queryString);

            var response = await _httpClient.GetAsync(url);
            await EnsureSuccessAsync(response, $"GET {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);

            _logger.LogDebug("GET request to {Table} returned {Count} records", table, result?.Count ?? 0);

            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error getting records from table {Table}", table);
            throw new SupabaseDbException($"Failed to get records from {table}", ex);
        }
    }

    /// <summary>
    /// Gets a single record from a table matching the specified filters.
    /// </summary>
    /// <typeparam name="T">The entity type to deserialize to.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="filters">Filters to identify the record.</param>
    /// <returns>The matching record, or null if not found.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<T?> GetSingleAsync<T>(
        string table,
        Dictionary<string, string> filters) where T : class
    {
        try
        {
            var results = await GetAsync<T>(table, filters, limit: 1);
            return results.FirstOrDefault();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error getting single record from table {Table}", table);
            throw new SupabaseDbException($"Failed to get record from {table}", ex);
        }
    }

    /// <summary>
    /// Gets a single record by its ID (assumes UUID primary key named 'id').
    /// </summary>
    /// <typeparam name="T">The entity type to deserialize to.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="id">The record ID.</param>
    /// <returns>The matching record, or null if not found.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<T?> GetByIdAsync<T>(string table, Guid id) where T : class
    {
        var filters = new Dictionary<string, string>
        {
            ["id"] = $"eq.{id}"
        };

        return await GetSingleAsync<T>(table, filters);
    }

    #endregion

    #region POST Operations

    /// <summary>
    /// Inserts a new record into a table.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="data">The data to insert.</param>
    /// <returns>The inserted record with server-generated values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<T> PostAsync<T>(string table, T data) where T : class
    {
        try
        {
            _logger.LogDebug("POST request to {Table}", table);

            var request = new HttpRequestMessage(HttpMethod.Post, $"rest/v1/{table}")
            {
                Content = JsonContent.Create(data, options: _jsonOptions)
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessAsync(response, $"POST {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
            var insertedRecord = result?.FirstOrDefault()
                ?? throw new SupabaseDbException("No data returned from insert");

            _logger.LogDebug("POST request to {Table} succeeded", table);

            return insertedRecord;
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error inserting record into table {Table}", table);
            throw new SupabaseDbException($"Failed to insert record into {table}", ex);
        }
    }

    /// <summary>
    /// Inserts multiple records into a table in a single batch operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="items">The items to insert.</param>
    /// <returns>The inserted records with server-generated values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<List<T>> InsertBatchAsync<T>(string table, List<T> items) where T : class
    {
        if (items == null || items.Count == 0)
        {
            return new List<T>();
        }

        try
        {
            _logger.LogDebug("Batch POST request to {Table} with {Count} items", table, items.Count);

            var request = new HttpRequestMessage(HttpMethod.Post, $"rest/v1/{table}")
            {
                Content = JsonContent.Create(items, options: _jsonOptions)
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessAsync(response, $"POST batch {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);

            _logger.LogDebug("Batch POST to {Table} succeeded, returned {Count} records",
                table, result?.Count ?? 0);

            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error inserting batch into table {Table}", table);
            throw new SupabaseDbException($"Failed to insert batch into {table}", ex);
        }
    }

    #endregion

    #region PATCH Operations

    /// <summary>
    /// Updates an existing record by ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="id">The record ID to update.</param>
    /// <param name="data">The updated data.</param>
    /// <returns>The updated record with server values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<T> UpdateByIdAsync<T>(string table, Guid id, T data) where T : class
    {
        var filters = new Dictionary<string, string>
        {
            ["id"] = $"eq.{id}"
        };

        var results = await UpdateAsync(table, filters, data);
        return results.FirstOrDefault()
            ?? throw new SupabaseDbException($"No data returned from update for ID {id}");
    }

    /// <summary>
    /// Updates records matching the specified filters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="filters">Filters to identify which records to update.</param>
    /// <param name="data">The updated data.</param>
    /// <returns>List of updated records with server values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<List<T>> UpdateAsync<T>(
        string table,
        Dictionary<string, string> filters,
        T data) where T : class
    {
        try
        {
            var queryString = BuildQueryString(filters);

            _logger.LogDebug("PATCH request to {Table}: {QueryString}", table, queryString);

            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"rest/v1/{table}{queryString}")
            {
                Content = JsonContent.Create(data, options: _jsonOptions)
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessAsync(response, $"PATCH {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);

            _logger.LogDebug("PATCH request to {Table} updated {Count} records",
                table, result?.Count ?? 0);

            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error updating records in table {Table}", table);
            throw new SupabaseDbException($"Failed to update records in {table}", ex);
        }
    }

    /// <summary>
    /// Updates multiple records with different data in a single batch operation.
    /// Uses upsert with merge-duplicates resolution.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="items">The items to update (must include ID field).</param>
    /// <returns>List of updated records with server values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<List<T>> UpdateBatchAsync<T>(string table, List<T> items) where T : class
    {
        if (items == null || items.Count == 0)
        {
            return new List<T>();
        }

        try
        {
            _logger.LogDebug("Batch UPSERT request to {Table} with {Count} items", table, items.Count);

            var request = new HttpRequestMessage(HttpMethod.Post, $"rest/v1/{table}")
            {
                Content = JsonContent.Create(items, options: _jsonOptions)
            };
            request.Headers.Add("Prefer", "resolution=merge-duplicates,return=representation");

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessAsync(response, $"UPSERT batch {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);

            _logger.LogDebug("Batch UPSERT to {Table} succeeded, returned {Count} records",
                table, result?.Count ?? 0);

            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error updating batch in table {Table}", table);
            throw new SupabaseDbException($"Failed to update batch in {table}", ex);
        }
    }

    #endregion

    #region DELETE Operations

    /// <summary>
    /// Deletes a record by ID.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="id">The record ID to delete.</param>
    /// <returns>True if the record was deleted, false if it wasn't found.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<bool> DeleteByIdAsync(string table, Guid id)
    {
        var filters = new Dictionary<string, string>
        {
            ["id"] = $"eq.{id}"
        };

        return await DeleteAsync(table, filters);
    }

    /// <summary>
    /// Deletes records matching the specified filters.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="filters">Filters to identify which records to delete.</param>
    /// <returns>True if records were deleted, false if none were found.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<bool> DeleteAsync(string table, Dictionary<string, string> filters)
    {
        try
        {
            var queryString = BuildQueryString(filters);

            _logger.LogDebug("DELETE request to {Table}: {QueryString}", table, queryString);

            var response = await _httpClient.DeleteAsync($"rest/v1/{table}{queryString}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("DELETE request to {Table} found no records to delete", table);
                return false;
            }

            await EnsureSuccessAsync(response, $"DELETE {table}");

            _logger.LogDebug("DELETE request to {Table} succeeded", table);

            return true;
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error deleting records from table {Table}", table);
            throw new SupabaseDbException($"Failed to delete records from {table}", ex);
        }
    }

    #endregion

    #region Upsert Operations

    /// <summary>
    /// Upserts (inserts or updates) multiple records in a single batch operation.
    /// Uses Supabase's bulk upsert with resolution=merge-duplicates.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="table">The table name.</param>
    /// <param name="data">The data to upsert.</param>
    /// <returns>List of upserted records with server values.</returns>
    /// <exception cref="SupabaseDbException">Thrown when the operation fails.</exception>
    public async Task<List<T>> UpsertBatchAsync<T>(string table, List<T> data) where T : class
    {
        if (data == null || data.Count == 0)
        {
            return new List<T>();
        }

        try
        {
            _logger.LogDebug("UPSERT request to {Table} with {Count} items", table, data.Count);

            var request = new HttpRequestMessage(HttpMethod.Post, $"rest/v1/{table}")
            {
                Content = JsonContent.Create(data, options: _jsonOptions)
            };
            request.Headers.Add("Prefer", "resolution=merge-duplicates,return=representation");

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessAsync(response, $"UPSERT {table}");

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);

            _logger.LogDebug("UPSERT to {Table} succeeded, returned {Count} records",
                table, result?.Count ?? 0);

            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is not SupabaseDbException)
        {
            _logger.LogError(ex, "Error upserting records in table {Table}", table);
            throw new SupabaseDbException($"Failed to upsert records in {table}", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds a query string from filters, ordering, and pagination parameters.
    /// </summary>
    /// <param name="filters">Optional filters using PostgREST operators.</param>
    /// <param name="orderBy">Optional ordering specification.</param>
    /// <param name="limit">Optional limit on number of records.</param>
    /// <param name="offset">Optional offset for pagination.</param>
    /// <returns>Query string with leading '?' if parameters exist, empty string otherwise.</returns>
    private string BuildQueryString(
        Dictionary<string, string>? filters = null,
        string? orderBy = null,
        int? limit = null,
        int? offset = null)
    {
        var queryParams = new List<string>();

        // Add filters
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                queryParams.Add($"{filter.Key}={Uri.EscapeDataString(filter.Value)}");
            }
        }

        // Add ordering
        if (!string.IsNullOrEmpty(orderBy))
        {
            queryParams.Add($"order={Uri.EscapeDataString(orderBy)}");
        }

        // Add limit
        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        // Add offset
        if (offset.HasValue)
        {
            queryParams.Add($"offset={offset.Value}");
        }

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
    }

    /// <summary>
    /// Ensures the HTTP response was successful, throwing typed exceptions for known error codes.
    /// </summary>
    /// <param name="response">The HTTP response to check.</param>
    /// <param name="operation">Description of the operation for logging.</param>
    /// <exception cref="SupabaseUnauthorizedException">Thrown for 401 Unauthorized responses.</exception>
    /// <exception cref="SupabaseForbiddenException">Thrown for 403 Forbidden responses (RLS violations).</exception>
    /// <exception cref="SupabaseNotFoundException">Thrown for 404 Not Found responses.</exception>
    /// <exception cref="SupabaseDbException">Thrown for other error responses.</exception>
    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var message = $"Supabase {operation} failed ({response.StatusCode})";

        _logger.LogError("{Message}: {Content}", message, content);

        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized =>
                new SupabaseUnauthorizedException(
                    "Unauthorized access to Supabase. Token may be missing or expired.",
                    content),
            System.Net.HttpStatusCode.Forbidden =>
                new SupabaseForbiddenException(
                    "Forbidden access to Supabase resource. Check Row-Level Security (RLS) policies.",
                    content),
            System.Net.HttpStatusCode.NotFound =>
                new SupabaseNotFoundException(
                    "Supabase resource not found.",
                    content),
            _ => new SupabaseDbException($"{message}: {content}", content)
        };
    }

    #endregion
}
