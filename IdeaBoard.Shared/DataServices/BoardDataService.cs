using IdeaBoard.Shared.DataEntities;
using IdeaBoard.Shared.Services;

namespace IdeaBoard.Shared.DataServices;

public class BoardDataService : BaseDataService<BoardEntity>
{
    public BoardDataService(SupabaseService supabaseService)
        : base(supabaseService)
    {
    }

    /// <summary>
    /// Gets the most recently updated boards.
    /// </summary>
    public async Task<List<BoardEntity>> GetRecentAsync(int count = 10)
    {
        var filter = $"order=updated_at.desc&limit={count}";
        return await SupabaseService.GetAsync<BoardEntity>(TableName, filter);
    }
}
