namespace IdeaBoard.Features.Boards.Models;

public class Board
{
    /// <summary>
    /// Board ID. Null before saving to database (will be auto-generated).
    /// </summary>
    public Guid? Id { get; set; }

    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp. Null before saving to database (will be auto-generated).
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp. Null before saving to database (will be auto-generated).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
