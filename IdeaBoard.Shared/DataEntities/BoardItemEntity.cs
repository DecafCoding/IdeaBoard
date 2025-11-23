using System.Text.Json.Serialization;

namespace IdeaBoard.Shared.DataEntities;

[Table("board_items")]
public class BoardItemEntity
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("board_id")]
    public Guid BoardId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty; // JSONB stored as string

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty; // JSONB stored as string

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty; // JSONB stored as string

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; } // JSONB stored as string, nullable

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
