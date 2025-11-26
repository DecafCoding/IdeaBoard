using IdeaBoard.Features.Boards.Models;
using IdeaBoard.Features.Boards.Services;
using IdeaBoard.Shared.Exceptions.Supabase;
using IdeaBoard.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using System.Security.Claims;

namespace IdeaBoard.Tests.Features.Boards.Services;

public class BoardServiceTests
{
    private readonly Mock<SupabaseService> _mockSupabaseService;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly BoardService _boardService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public BoardServiceTests()
    {
        _mockSupabaseService = new Mock<SupabaseService>(
            MockBehavior.Strict,
            null!, // SupabaseHttpClient - not used in mocked scenarios
            null!  // ILogger - not used in mocked scenarios
        );

        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

        _boardService = new BoardService(
            _mockSupabaseService.Object,
            _mockAuthStateProvider.Object
        );
    }

    #region CreateBoardAsync Tests

    [Fact]
    public async Task CreateBoardAsync_ValidName_ReturnsCreatedBoard()
    {
        // Arrange
        var boardName = "My Test Board";
        var expectedBoard = new Board
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Name = boardName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        SetupAuthenticatedUser(_testUserId);

        _mockSupabaseService
            .Setup(s => s.PostAsync(
                "boards",
                It.Is<Board>(b =>
                    b.UserId == _testUserId &&
                    b.Name == boardName)))
            .ReturnsAsync(expectedBoard);

        // Act
        var result = await _boardService.CreateBoardAsync(boardName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBoard.Id, result.Id);
        Assert.Equal(expectedBoard.UserId, result.UserId);
        Assert.Equal(expectedBoard.Name, result.Name);
        Assert.Equal(expectedBoard.CreatedAt, result.CreatedAt);
        Assert.Equal(expectedBoard.UpdatedAt, result.UpdatedAt);

        _mockSupabaseService.Verify(
            s => s.PostAsync("boards", It.IsAny<Board>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateBoardAsync_TrimsWhitespace_ReturnsCreatedBoard()
    {
        // Arrange
        var boardNameWithWhitespace = "  Test Board  ";
        var trimmedName = "Test Board";
        var expectedBoard = new Board
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        SetupAuthenticatedUser(_testUserId);

        _mockSupabaseService
            .Setup(s => s.PostAsync(
                "boards",
                It.Is<Board>(b =>
                    b.UserId == _testUserId &&
                    b.Name == trimmedName)))
            .ReturnsAsync(expectedBoard);

        // Act
        var result = await _boardService.CreateBoardAsync(boardNameWithWhitespace);

        // Assert
        Assert.Equal(trimmedName, result.Name);
    }

    [Fact]
    public async Task CreateBoardAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var emptyName = "";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _boardService.CreateBoardAsync(emptyName)
        );

        Assert.Contains("Board name cannot be empty", exception.Message);
    }

    [Fact]
    public async Task CreateBoardAsync_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var whitespaceName = "   ";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _boardService.CreateBoardAsync(whitespaceName)
        );

        Assert.Contains("Board name cannot be empty", exception.Message);
    }

    [Fact]
    public async Task CreateBoardAsync_NullName_ThrowsArgumentException()
    {
        // Arrange
        string? nullName = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _boardService.CreateBoardAsync(nullName!)
        );
    }

    [Fact]
    public async Task CreateBoardAsync_NotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var boardName = "Test Board";
        SetupUnauthenticatedUser();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _boardService.CreateBoardAsync(boardName)
        );

        Assert.Contains("User is not authenticated", exception.Message);

        // Verify SupabaseService was never called
        _mockSupabaseService.Verify(
            s => s.PostAsync(It.IsAny<string>(), It.IsAny<Board>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateBoardAsync_SupabaseFailure_ThrowsSupabaseDbException()
    {
        // Arrange
        var boardName = "Test Board";
        SetupAuthenticatedUser(_testUserId);

        _mockSupabaseService
            .Setup(s => s.PostAsync("boards", It.IsAny<Board>()))
            .ThrowsAsync(new SupabaseDbException("Database error", "Error details"));

        // Act & Assert
        await Assert.ThrowsAsync<SupabaseDbException>(
            () => _boardService.CreateBoardAsync(boardName)
        );
    }

    [Fact]
    public async Task CreateBoardAsync_MaxLengthName_ReturnsCreatedBoard()
    {
        // Arrange
        var longName = new string('A', 100); // 100 characters
        var expectedBoard = new Board
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Name = longName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        SetupAuthenticatedUser(_testUserId);

        _mockSupabaseService
            .Setup(s => s.PostAsync("boards", It.IsAny<Board>()))
            .ReturnsAsync(expectedBoard);

        // Act
        var result = await _boardService.CreateBoardAsync(longName);

        // Assert
        Assert.Equal(longName, result.Name);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(claimsPrincipal);

        _mockAuthStateProvider
            .Setup(p => p.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }

    private void SetupUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity(); // No authentication type
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(claimsPrincipal);

        _mockAuthStateProvider
            .Setup(p => p.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }

    #endregion
}
