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
    /// Batch update multiple items (useful for canvas pan/zoom operations).
    /// </summary>
    public async Task<List<BoardItemEntity>> UpdateBatchAsync(List<BoardItemEntity> items)
    {
        var updatedItems = new List<BoardItemEntity>();

        foreach (var item in items)
        {
            var updated = await UpdateAsync(item.Id, item);
            updatedItems.Add(updated);
        }

        return updatedItems;
    }
}
