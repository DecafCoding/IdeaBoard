using IdeaBoard.Shared.Exceptions.Supabase;
using IdeaBoard.Shared.Services;
using IdeaBoard.Shared.Services.Supabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace IdeaBoard.Tests.Services.Supabase;

public class SupabaseServiceTests
{
    private readonly Mock<ILogger<SupabaseService>> _mockLogger;
    private readonly SupabaseHttpClient _supabaseHttpClient;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public SupabaseServiceTests()
    {
        _mockLogger = new Mock<ILogger<SupabaseService>>();

        // Create mock HttpMessageHandler for intercepting HTTP requests
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Create HttpClient with mock handler
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.supabase.co/")
        };

        // Create configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:AnonKey"] = "test-key"
            })
            .Build();

        // Note: We need to work around SupabaseHttpClient creating its own HttpClient
        // For now, we'll test with actual HTTP calls or mock at a higher level
        _supabaseHttpClient = new SupabaseHttpClient(httpClient, config);
    }

    private SupabaseService CreateService()
    {
        return new SupabaseService(_supabaseHttpClient, _mockLogger.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    #region GET Operations Tests

    [Fact]
    public async Task GetAsync_WithNoFilters_ReturnsRecords()
    {
        // Arrange
        var service = CreateService();
        var expectedData = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };
        var json = JsonSerializer.Serialize(expectedData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.GetAsync<TestEntity>("test_table");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAsync_WithFilters_BuildsCorrectQueryString()
    {
        // Arrange
        var service = CreateService();
        var filters = new Dictionary<string, string>
        {
            ["id"] = "eq.123",
            ["status"] = "eq.active"
        };

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        // Act
        await service.GetAsync<TestEntity>("test_table", filters);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("id=eq.123", capturedRequest.RequestUri?.Query);
        Assert.Contains("status=eq.active", capturedRequest.RequestUri?.Query);
    }

    [Fact]
    public async Task GetAsync_WithOrderByAndLimit_BuildsCorrectQueryString()
    {
        // Arrange
        var service = CreateService();

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        // Act
        await service.GetAsync<TestEntity>(
            "test_table",
            filters: null,
            orderBy: "created_at.desc",
            limit: 10,
            offset: 5);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("order=created_at.desc", capturedRequest.RequestUri?.Query);
        Assert.Contains("limit=10", capturedRequest.RequestUri?.Query);
        Assert.Contains("offset=5", capturedRequest.RequestUri?.Query);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsRecord()
    {
        // Arrange
        var service = CreateService();
        var testId = Guid.NewGuid();
        var expectedData = new List<TestEntity>
        {
            new() { Id = testId, Name = "Test" }
        };
        var json = JsonSerializer.Serialize(expectedData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.GetByIdAsync<TestEntity>("test_table", testId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testId, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetSingleAsync_WithFilters_ReturnsFirstRecord()
    {
        // Arrange
        var service = CreateService();
        var filters = new Dictionary<string, string> { ["name"] = "eq.Test" };
        var expectedData = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test" }
        };
        var json = JsonSerializer.Serialize(expectedData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.GetSingleAsync<TestEntity>("test_table", filters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    #endregion

    #region POST Operations Tests

    [Fact]
    public async Task PostAsync_WithValidData_ReturnsInsertedRecord()
    {
        // Arrange
        var service = CreateService();
        var newEntity = new TestEntity { Name = "New Test" };
        var insertedEntity = new TestEntity { Id = Guid.NewGuid(), Name = "New Test" };
        var json = JsonSerializer.Serialize(new[] { insertedEntity }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.Created, json);

        // Act
        var result = await service.PostAsync("test_table", newEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Test", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task InsertBatchAsync_WithMultipleItems_ReturnsInsertedRecords()
    {
        // Arrange
        var service = CreateService();
        var newEntities = new List<TestEntity>
        {
            new() { Name = "Test1" },
            new() { Name = "Test2" }
        };
        var insertedEntities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };
        var json = JsonSerializer.Serialize(insertedEntities, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.Created, json);

        // Act
        var result = await service.InsertBatchAsync("test_table", newEntities);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task InsertBatchAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var emptyList = new List<TestEntity>();

        // Act
        var result = await service.InsertBatchAsync("test_table", emptyList);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region PATCH Operations Tests

    [Fact]
    public async Task UpdateByIdAsync_WithValidData_ReturnsUpdatedRecord()
    {
        // Arrange
        var service = CreateService();
        var testId = Guid.NewGuid();
        var updateData = new TestEntity { Name = "Updated Test" };
        var updatedEntity = new TestEntity { Id = testId, Name = "Updated Test" };
        var json = JsonSerializer.Serialize(new[] { updatedEntity }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.UpdateByIdAsync("test_table", testId, updateData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Test", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithFilters_ReturnsUpdatedRecords()
    {
        // Arrange
        var service = CreateService();
        var filters = new Dictionary<string, string> { ["status"] = "eq.pending" };
        var updateData = new { Status = "active" };
        var updatedEntities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };
        var json = JsonSerializer.Serialize(updatedEntities, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.UpdateAsync("test_table", filters, updateData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateBatchAsync_WithMultipleItems_ReturnsUpdatedRecords()
    {
        // Arrange
        var service = CreateService();
        var updateItems = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Updated1" },
            new() { Id = Guid.NewGuid(), Name = "Updated2" }
        };
        var json = JsonSerializer.Serialize(updateItems, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.UpdateBatchAsync("test_table", updateItems);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region DELETE Operations Tests

    [Fact]
    public async Task DeleteByIdAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var testId = Guid.NewGuid();

        SetupHttpResponse(HttpStatusCode.NoContent, string.Empty);

        // Act
        var result = await service.DeleteByIdAsync("test_table", testId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteByIdAsync_WithNotFoundId_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var testId = Guid.NewGuid();

        SetupHttpResponse(HttpStatusCode.NotFound, string.Empty);

        // Act
        var result = await service.DeleteByIdAsync("test_table", testId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithFilters_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var filters = new Dictionary<string, string> { ["status"] = "eq.deleted" };

        SetupHttpResponse(HttpStatusCode.NoContent, string.Empty);

        // Act
        var result = await service.DeleteAsync("test_table", filters);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Upsert Operations Tests

    [Fact]
    public async Task UpsertBatchAsync_WithMultipleItems_ReturnsUpsertedRecords()
    {
        // Arrange
        var service = CreateService();
        var upsertItems = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Upsert1" },
            new() { Id = Guid.NewGuid(), Name = "Upsert2" }
        };
        var json = JsonSerializer.Serialize(upsertItems, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        SetupHttpResponse(HttpStatusCode.OK, json);

        // Act
        var result = await service.UpsertBatchAsync("test_table", upsertItems);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpsertBatchAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var emptyList = new List<TestEntity>();

        // Act
        var result = await service.UpsertBatchAsync("test_table", emptyList);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task GetAsync_WithUnauthorized_ThrowsSupabaseUnauthorizedException()
    {
        // Arrange
        var service = CreateService();
        SetupHttpResponse(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SupabaseUnauthorizedException>(
            () => service.GetAsync<TestEntity>("test_table"));

        Assert.Contains("Unauthorized", exception.Message);
        Assert.NotNull(exception.ResponseContent);
    }

    [Fact]
    public async Task GetAsync_WithForbidden_ThrowsSupabaseForbiddenException()
    {
        // Arrange
        var service = CreateService();
        SetupHttpResponse(HttpStatusCode.Forbidden, "{\"error\":\"RLS policy violation\"}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SupabaseForbiddenException>(
            () => service.GetAsync<TestEntity>("test_table"));

        Assert.Contains("Forbidden", exception.Message);
        Assert.Contains("RLS", exception.Message);
        Assert.NotNull(exception.ResponseContent);
    }

    [Fact]
    public async Task GetAsync_WithNotFound_ThrowsSupabaseNotFoundException()
    {
        // Arrange
        var service = CreateService();
        SetupHttpResponse(HttpStatusCode.NotFound, "{\"error\":\"Not found\"}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SupabaseNotFoundException>(
            () => service.GetAsync<TestEntity>("test_table"));

        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(exception.ResponseContent);
    }

    [Fact]
    public async Task PostAsync_WithBadRequest_ThrowsSupabaseDbException()
    {
        // Arrange
        var service = CreateService();
        var newEntity = new TestEntity { Name = "Test" };
        SetupHttpResponse(HttpStatusCode.BadRequest, "{\"error\":\"Invalid data\"}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SupabaseDbException>(
            () => service.PostAsync("test_table", newEntity));

        Assert.Contains("Failed", exception.Message);
    }

    #endregion

    #region Helper Test Entity

    private class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
