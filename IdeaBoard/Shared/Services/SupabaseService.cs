namespace IdeaBoard.Shared.Services;

public class SupabaseService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SupabaseService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        _httpClient.BaseAddress = new Uri(supabaseUrl!);
        _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
    }

    // TODO: Implement base HTTP methods (Story F1.7)
    // - GetAsync<T>
    // - PostAsync<T>
    // - PutAsync<T>
    // - PatchAsync<T>
    // - DeleteAsync
}
