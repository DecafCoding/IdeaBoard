namespace IdeaBoard.Features.BoardItems.Models;

public class BoardItem
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid UserId { get; set; }
    public string ItemType { get; set; } = string.Empty; // "note", "image", "link", "todo"
    public ItemPosition Position { get; set; } = new();
    public ItemSize Size { get; set; } = new();
    public Dictionary<string, object> Content { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ItemPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public int ZIndex { get; set; }
}

public class ItemSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}
