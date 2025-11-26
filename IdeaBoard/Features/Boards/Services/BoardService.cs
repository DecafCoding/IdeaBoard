using IdeaBoard.Features.Boards.Models;
using IdeaBoard.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace IdeaBoard.Features.Boards.Services;

public class BoardService
{
    private readonly SupabaseService _supabaseService;
    private readonly AuthenticationStateProvider _authStateProvider;

    public BoardService(
        SupabaseService supabaseService,
        AuthenticationStateProvider authStateProvider)
    {
        _supabaseService = supabaseService;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Gets all boards for the current user, ordered by last updated (newest first).
    /// </summary>
    public async Task<List<Board>> GetBoardsAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Query Supabase: GET /rest/v1/boards?user_id=eq.{userId}&order=updated_at.desc
        var filters = new Dictionary<string, string>
        {
            ["user_id"] = $"eq.{userId}"
        };
        return await _supabaseService.GetAsync<Board>("boards", filters, orderBy: "updated_at.desc");
    }

    // TODO: Implement remaining CRUD methods (Stories B3.2-B3.4)
    // - GetBoardByIdAsync
    // - CreateBoardAsync
    // - UpdateBoardAsync
    // - DeleteBoardAsync

    /// <summary>
    /// Gets the current authenticated user's ID from claims.
    /// </summary>
    private async Task<Guid> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }

        return Guid.Empty;
    }
}
