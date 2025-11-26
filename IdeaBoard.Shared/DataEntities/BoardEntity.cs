using System.Text.Json.Serialization;

namespace IdeaBoard.Shared.DataEntities;

[Table("boards")]
public class BoardEntity
{
    /// <summary>
    /// Board ID. Null before saving to database (will be auto-generated).
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Created timestamp. Null before saving to database (will be auto-generated).
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp. Auto-updated by database trigger.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
