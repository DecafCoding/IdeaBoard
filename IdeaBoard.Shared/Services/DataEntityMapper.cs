using System.Text.Json;
using IdeaBoard.Shared.Models.Board;
using IdeaBoard.Shared.Models.Canvas;
using IdeaBoard.Shared.DataEntities;

namespace IdeaBoard.Shared.Services;

public class DataEntityMapper
{
    private readonly JsonSerializerOptions _jsonOptions;

    public DataEntityMapper()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region Board Mapping

    /// <summary>
    /// Maps a BoardEntity (database) to a Board (domain model).
    /// </summary>
    public Board MapToBoard(BoardEntity entity)
    {
        return new Board
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps a Board (domain model) to a BoardEntity (database).
    /// </summary>
    public BoardEntity MapToBoardEntity(Board model)
    {
        return new BoardEntity
        {
            Id = model.Id,
            UserId = model.UserId,
            Name = model.Name,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    #endregion

    #region BoardItem Mapping

    /// <summary>
    /// Maps a BoardItemEntity (database) to a BoardItem (domain model).
    /// Deserializes JSONB string fields to strongly-typed objects.
    /// </summary>
    public BoardItem MapToBoardItem(BoardItemEntity entity)
    {
        return new BoardItem
        {
            Id = entity.Id,
            BoardId = entity.BoardId,
            UserId = entity.UserId,
            ItemType = entity.ItemType,
            Position = DeserializeJsonField<ItemPosition>(entity.Position) ?? new ItemPosition(),
            Size = DeserializeJsonField<ItemSize>(entity.Size) ?? new ItemSize(),
            Content = DeserializeJsonField<Dictionary<string, object>>(entity.Content) ?? new Dictionary<string, object>(),
            Metadata = DeserializeJsonField<Dictionary<string, object>>(entity.Metadata) ?? new Dictionary<string, object>(),
            CreatedAt = entity.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps a BoardItem (domain model) to a BoardItemEntity (database).
    /// Serializes strongly-typed objects to JSONB string fields.
    /// </summary>
    public BoardItemEntity MapToBoardItemEntity(BoardItem model)
    {
        return new BoardItemEntity
        {
            Id = model.Id,
            BoardId = model.BoardId,
            UserId = model.UserId,
            ItemType = model.ItemType,
            Position = SerializeJsonField(model.Position),
            Size = SerializeJsonField(model.Size),
            Content = SerializeJsonField(model.Content),
            Metadata = SerializeJsonField(model.Metadata),
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    #endregion

    #region Batch Mapping

    /// <summary>
    /// Maps a list of BoardEntities to Boards.
    /// </summary>
    public List<Board> MapToBoards(List<BoardEntity> entities)
    {
        return entities.Select(MapToBoard).ToList();
    }

    /// <summary>
    /// Maps a list of BoardItemEntities to BoardItems.
    /// </summary>
    public List<BoardItem> MapToBoardItems(List<BoardItemEntity> entities)
    {
        return entities.Select(MapToBoardItem).ToList();
    }

    #endregion

    #region JSON Serialization Helpers

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    private string SerializeJsonField<T>(T value)
    {
        if (value == null)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    private T? DeserializeJsonField<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            // Log error in production
            return default;
        }
    }

    #endregion
}
