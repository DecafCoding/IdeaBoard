using IdeaBoard.Features.BoardItems.Models;
using IdeaBoard.Shared.DataServices;
using Microsoft.Extensions.Configuration;

namespace IdeaBoard.Shared.Services;

/// <summary>
/// Manages canvas state with optimistic updates and auto-save functionality.
/// This is the source of truth for canvas items in Blazor.
/// </summary>
public class CanvasStateService : IDisposable
{
    private readonly BoardItemDataService _boardItemDataService;
    private readonly DataEntityMapper _mapper;
    private readonly int _autoSaveDebounceMs;
    private readonly ConnectionStateService _connectionStateService;

    private List<BoardItem> _items = new();
    private HashSet<Guid> _dirtyItemIds = new();
    private List<Guid> _selectedItemIds = new();
    private Timer? _autoSaveTimer;
    private Guid? _currentBoardId;

    // Events
    public event Action? ItemsChanged;
    public event Action<List<Guid>>? SelectionChanged;

    public CanvasStateService(
        BoardItemDataService boardItemDataService,
        DataEntityMapper mapper,
        ConnectionStateService connectionStateService,
        IConfiguration configuration)
    {
        _boardItemDataService = boardItemDataService;
        _mapper = mapper;
        _connectionStateService = connectionStateService;
        _autoSaveDebounceMs = configuration.GetValue<int>("Canvas:AutoSaveDebounceMs", 1000);
    }

    /// <summary>
    /// Gets all items currently loaded in the canvas.
    /// </summary>
    public IReadOnlyList<BoardItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the currently selected item IDs.
    /// </summary>
    public IReadOnlyList<Guid> SelectedItemIds => _selectedItemIds.AsReadOnly();

    /// <summary>
    /// Gets the number of unsaved changes.
    /// </summary>
    public int UnsavedChangesCount => _dirtyItemIds.Count;

    /// <summary>
    /// Loads items for a specific board from the database.
    /// </summary>
    public async Task LoadItemsAsync(Guid boardId)
    {
        _currentBoardId = boardId;

        try
        {
            var entities = await _boardItemDataService.GetByBoardIdAsync(boardId);
            _items = _mapper.MapToBoardItems(entities);
            _dirtyItemIds.Clear();
            _selectedItemIds.Clear();

            ItemsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load items: {ex.Message}");
            _connectionStateService.SetConnectionState(false);
            throw;
        }
    }

    /// <summary>
    /// Adds a new item to the canvas (optimistic).
    /// </summary>
    public void AddItem(BoardItem item)
    {
        _items.Add(item);
        _dirtyItemIds.Add(item.Id);

        ItemsChanged?.Invoke();
        TriggerAutoSave();
    }

    /// <summary>
    /// Updates an item's position optimistically.
    /// </summary>
    public void UpdateItemPositionOptimistic(Guid itemId, double x, double y)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Position.X = x;
            item.Position.Y = y;
            item.UpdatedAt = DateTime.UtcNow;

            _dirtyItemIds.Add(itemId);

            ItemsChanged?.Invoke();
            TriggerAutoSave();
        }
    }

    /// <summary>
    /// Updates an item's size optimistically.
    /// </summary>
    public void UpdateItemSizeOptimistic(Guid itemId, double width, double height)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Size.Width = width;
            item.Size.Height = height;
            item.UpdatedAt = DateTime.UtcNow;

            _dirtyItemIds.Add(itemId);

            ItemsChanged?.Invoke();
            TriggerAutoSave();
        }
    }

    /// <summary>
    /// Updates an item's content optimistically.
    /// </summary>
    public void UpdateItemContentOptimistic(Guid itemId, Dictionary<string, object> content)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Content = content;
            item.UpdatedAt = DateTime.UtcNow;

            _dirtyItemIds.Add(itemId);

            ItemsChanged?.Invoke();
            TriggerAutoSave();
        }
    }

    /// <summary>
    /// Removes an item from the canvas (optimistic).
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            _dirtyItemIds.Remove(itemId);
            _selectedItemIds.Remove(itemId);

            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke(_selectedItemIds);

            // Delete immediately (not batched)
            _ = DeleteItemAsync(itemId);
        }
    }

    /// <summary>
    /// Updates the selection state.
    /// </summary>
    public void UpdateSelection(List<Guid> selectedIds)
    {
        _selectedItemIds = selectedIds;
        SelectionChanged?.Invoke(_selectedItemIds);
    }

    /// <summary>
    /// Deletes selected items.
    /// </summary>
    public void DeleteSelectedItems()
    {
        var itemsToDelete = _selectedItemIds.ToList();
        foreach (var itemId in itemsToDelete)
        {
            RemoveItem(itemId);
        }
    }

    /// <summary>
    /// Triggers the auto-save timer (debounced).
    /// </summary>
    private void TriggerAutoSave()
    {
        _connectionStateService.SetHasUnsavedChanges(_dirtyItemIds.Count > 0);

        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(
            async _ => await BatchSaveAsync(),
            null,
            _autoSaveDebounceMs,
            Timeout.Infinite);
    }

    /// <summary>
    /// Saves all dirty items to the database in a batch.
    /// </summary>
    public async Task BatchSaveAsync()
    {
        if (_dirtyItemIds.Count == 0)
        {
            return;
        }

        var itemsToSave = _items
            .Where(i => _dirtyItemIds.Contains(i.Id))
            .ToList();

        try
        {
            // Convert to entities
            var entities = itemsToSave
                .Select(i => _mapper.MapToBoardItemEntity(i))
                .ToList();

            // Batch update
            await _boardItemDataService.UpdateBatchAsync(entities);

            // Clear dirty flags on success
            _dirtyItemIds.Clear();
            _connectionStateService.SetConnectionState(true);
            _connectionStateService.SetHasUnsavedChanges(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auto-save failed: {ex.Message}");
            _connectionStateService.SetConnectionState(false);
            // Keep dirty items for retry on next change
        }
    }

    /// <summary>
    /// Deletes an item from the database.
    /// </summary>
    private async Task DeleteItemAsync(Guid itemId)
    {
        try
        {
            await _boardItemDataService.DeleteAsync(itemId);
            _connectionStateService.SetConnectionState(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete item: {ex.Message}");
            _connectionStateService.SetConnectionState(false);
        }
    }

    /// <summary>
    /// Forces an immediate save of all pending changes.
    /// </summary>
    public async Task SaveNowAsync()
    {
        _autoSaveTimer?.Dispose();
        await BatchSaveAsync();
    }

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
    }
}
