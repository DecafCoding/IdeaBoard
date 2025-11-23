using IdeaBoard.Features.BoardItems.Models;
using Microsoft.JSInterop;

namespace IdeaBoard.Shared.Services;

/// <summary>
/// JSInterop bridge between JavaScript canvas and Blazor state management.
/// Handles bidirectional communication.
/// </summary>
public class CanvasInteropService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private readonly CanvasStateService _canvasStateService;
    private DotNetObjectReference<CanvasInteropService>? _dotNetReference;

    public CanvasInteropService(IJSRuntime jsRuntime, CanvasStateService canvasStateService)
    {
        _canvasStateService = canvasStateService;
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "/js/features/canvas/canvas.js").AsTask());
    }

    /// <summary>
    /// Initializes the canvas with pan/zoom and items.
    /// </summary>
    public async Task InitializeCanvasAsync(string canvasElementId, Guid boardId, List<BoardItem> items)
    {
        var module = await _moduleTask.Value;
        _dotNetReference = DotNetObjectReference.Create(this);

        var element = await GetElementReferenceAsync(canvasElementId);
        await module.InvokeVoidAsync("initialize", element, _dotNetReference, boardId.ToString(), items);
    }

    /// <summary>
    /// Adds a new item to the canvas.
    /// </summary>
    public async Task AddItemAsync(BoardItem item)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("addItem", item);
    }

    /// <summary>
    /// Updates an existing item on the canvas.
    /// </summary>
    public async Task UpdateItemAsync(BoardItem item)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateItem", item);
    }

    /// <summary>
    /// Removes an item from the canvas.
    /// </summary>
    public async Task RemoveItemAsync(Guid itemId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("removeItem", itemId.ToString());
    }

    /// <summary>
    /// Gets the current selection.
    /// </summary>
    public async Task<string[]> GetSelectionAsync()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string[]>("getSelection");
    }

    /// <summary>
    /// Sets the selection programmatically.
    /// </summary>
    public async Task SetSelectionAsync(List<Guid> itemIds)
    {
        var module = await _moduleTask.Value;
        var idStrings = itemIds.Select(id => id.ToString()).ToArray();
        await module.InvokeVoidAsync("setSelection", idStrings);
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearSelection");
    }

    /// <summary>
    /// Gets the current pan/zoom state.
    /// </summary>
    public async Task<PanZoomState> GetPanZoomStateAsync()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<PanZoomState>("getPanZoomState");
    }

    /// <summary>
    /// Destroys the canvas and cleans up resources.
    /// </summary>
    public async Task DestroyCanvasAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("destroy");
        }
    }

    /// <summary>
    /// Called by JavaScript when an item drag operation ends.
    /// </summary>
    [JSInvokable]
    public void OnItemDragEnd(string itemId, double x, double y)
    {
        if (Guid.TryParse(itemId, out var id))
        {
            _canvasStateService.UpdateItemPositionOptimistic(id, x, y);
        }
    }

    /// <summary>
    /// Called by JavaScript when the selection changes.
    /// </summary>
    [JSInvokable]
    public void OnSelectionChanged(string[] itemIds)
    {
        var guids = itemIds
            .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
            .Where(guid => guid != Guid.Empty)
            .ToList();

        _canvasStateService.UpdateSelection(guids);
    }

    /// <summary>
    /// Helper to get element reference by ID.
    /// </summary>
    private async Task<IJSObjectReference> GetElementReferenceAsync(string elementId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<IJSObjectReference>(
            "eval", $"document.getElementById('{elementId}')");
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }

        _dotNetReference?.Dispose();
    }
}

/// <summary>
/// Represents the pan/zoom state of the canvas.
/// </summary>
public class PanZoomState
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; }
}
