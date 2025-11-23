using Microsoft.Extensions.Configuration;
using IdeaBoard.Shared.Services.Supabase;

namespace IdeaBoard.Tests.Services.Supabase;

public class SupabaseHttpClientTests
{
    [Fact]
    public void Constructor_WithValidConfig_InitializesSuccessfully()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:AnonKey"] = "test-key"
            })
            .Build();

        var httpClient = new HttpClient();

        // Act
        var service = new SupabaseHttpClient(httpClient, config);

        // Assert
        Assert.NotNull(service);
        Assert.Equal("https://test.supabase.co/", service.Client.BaseAddress?.ToString());
        Assert.Equal("https://test.supabase.co", service.SupabaseUrl);
        Assert.Equal("test-key", service.AnonKey);
        Assert.Contains(service.Client.DefaultRequestHeaders,
            h => h.Key == "apikey" && h.Value.First() == "test-key");
    }

    [Fact]
    public void Constructor_WithMissingUrl_ThrowsException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:AnonKey"] = "test-key"
            })
            .Build();

        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new SupabaseHttpClient(httpClient, config));

        Assert.Contains("Supabase URL not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingAnonKey_ThrowsException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = "https://test.supabase.co"
            })
            .Build();

        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new SupabaseHttpClient(httpClient, config));

        Assert.Contains("Supabase Anon Key not configured", exception.Message);
    }

    [Fact]
    public void Constructor_SetsCorrectTimeout()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:AnonKey"] = "test-key"
            })
            .Build();

        var httpClient = new HttpClient();

        // Act
        var service = new SupabaseHttpClient(httpClient, config);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), service.Client.Timeout);
    }

    [Fact]
    public void Constructor_SetsAcceptHeader()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:AnonKey"] = "test-key"
            })
            .Build();

        var httpClient = new HttpClient();

        // Act
        var service = new SupabaseHttpClient(httpClient, config);

        // Assert
        Assert.Contains(service.Client.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }

    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        // Arrange
        var expectedUrl = "https://myproject.supabase.co";
        var expectedKey = "my-secret-key";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = expectedUrl,
                ["Supabase:AnonKey"] = expectedKey
            })
            .Build();

        var httpClient = new HttpClient();

        // Act
        var service = new SupabaseHttpClient(httpClient, config);

        // Assert
        Assert.Equal(expectedUrl, service.SupabaseUrl);
        Assert.Equal(expectedKey, service.AnonKey);
        Assert.Same(httpClient, service.Client);
    }
}
