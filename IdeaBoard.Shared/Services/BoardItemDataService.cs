using IdeaBoard.Shared.DataEntities;
using IdeaBoard.Shared.Services;

namespace IdeaBoard.Shared.Services;

public class BoardItemDataService : BaseDataService<BoardItemEntity>
{
    public BoardItemDataService(SupabaseService supabaseService)
        : base(supabaseService)
    {
    }

    /// <summary>
    /// Gets all items for a specific board (RLS enforced by auth token).
    /// </summary>
    public async Task<List<BoardItemEntity>> GetByBoardIdAsync(Guid boardId)
    {
        var filter = $"board_id=eq.{boardId}";
        return await SupabaseService.GetAsync<BoardItemEntity>(TableName, filter);
    }

    /// <summary>
    /// Batch update/insert multiple items using Supabase bulk upsert.
    /// Updates timestamps and returns server versions with updated timestamps.
    /// </summary>
    public async Task<List<BoardItemEntity>> UpdateBatchAsync(List<BoardItemEntity> items)
    {
        if (items == null || items.Count == 0)
        {
            return new List<BoardItemEntity>();
        }

        // Update all timestamps before sending
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.UpdatedAt = now;
        }

        // Use bulk upsert operation
        var updatedItems = await SupabaseService.UpsertBatchAsync(TableName, items);

        return updatedItems;
    }
}
