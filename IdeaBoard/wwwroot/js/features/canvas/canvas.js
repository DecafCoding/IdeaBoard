// Main canvas interaction module
// This handles pan, zoom, drag, and other canvas operations

let panzoomInstance = null;
let dotnetHelper = null;
let currentBoardId = null;
let canvasElement = null;

// Drag state
let draggedItem = null;
let dragOffset = { x: 0, y: 0 };
let isDragging = false;

// Resize state
let resizedItem = null;
let resizeHandle = null;
let isResizing = false;

// Selection state
let selectedItemIds = new Set();

// Item elements cache
let itemElements = new Map();

const VIEWPORT_STORAGE_KEY = 'canvas-viewport-';

/**
 * Initialize the canvas with pan/zoom and items
 */
export function initialize(element, dotnet, boardId, items) {
    canvasElement = element;
    dotnetHelper = dotnet;
    currentBoardId = boardId;

    // Initialize panzoom
    if (window.Panzoom) {
        panzoomInstance = window.Panzoom(element, {
            maxScale: 3.0,
            minScale: 0.1,
            step: 0.1,
            exclude: ['.canvas-item'], // Don't pan when dragging items
            cursor: 'grab'
        });

        // Restore viewport from localStorage
        restoreViewport();

        // Save viewport on change (debounced)
        let saveTimer = null;
        element.addEventListener('panzoomchange', () => {
            clearTimeout(saveTimer);
            saveTimer = setTimeout(() => {
                saveViewport();
            }, 300);
        });
    } else {
        console.error('Panzoom library not loaded');
    }

    // Add canvas-level click handler for deselection
    element.addEventListener('click', handleCanvasClick);

    // Render initial items
    if (items && items.length > 0) {
        items.forEach(item => addItem(item));
    }

    return true;
}

/**
 * Add a new item to the canvas
 */
export function addItem(item) {
    const itemElement = document.getElementById(`item-${item.id}`);
    if (itemElement) {
        enableItemDrag(itemElement, item.id);
        addResizeHandles(itemElement, item.id);
        itemElements.set(item.id, itemElement);
    }
}

/**
 * Add resize handles to an item
 */
function addResizeHandles(itemElement, itemId) {
    // Remove existing handles if any
    const existingHandles = itemElement.querySelectorAll('.resize-handle');
    existingHandles.forEach(handle => handle.remove());

    // Create 8 resize handles (corners + edges)
    const handles = ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w'];
    handles.forEach(position => {
        const handle = document.createElement('div');
        handle.className = `resize-handle resize-handle-${position}`;
        handle.dataset.position = position;
        handle.dataset.itemId = itemId;

        // Prevent drag when grabbing resize handle
        handle.addEventListener('mousedown', (e) => handleResizeMouseDown(e, itemElement, itemId, position));

        itemElement.appendChild(handle);
    });
}

/**
 * Update an existing item
 */
export function updateItem(item) {
    const itemElement = itemElements.get(item.id);
    if (itemElement) {
        // Update position
        itemElement.style.left = `${item.position.x}px`;
        itemElement.style.top = `${item.position.y}px`;
        itemElement.style.zIndex = item.position.zIndex;

        // Update size
        itemElement.style.width = `${item.size.width}px`;
        itemElement.style.height = `${item.size.height}px`;
    }
}

/**
 * Remove an item from the canvas
 */
export function removeItem(itemId) {
    itemElements.delete(itemId);
    selectedItemIds.delete(itemId);
}

/**
 * Enable drag functionality for an item
 */
export function enableItemDrag(itemElement, itemId) {
    itemElement.addEventListener('mousedown', (e) => handleItemMouseDown(e, itemElement, itemId));
    itemElement.addEventListener('click', (e) => handleItemClick(e, itemId));
}

/**
 * Handle item mousedown for drag start
 */
function handleItemMouseDown(e, itemElement, itemId) {
    // Don't start drag if clicking on interactive elements
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'BUTTON') {
        return;
    }

    e.stopPropagation();
    e.preventDefault();

    draggedItem = {
        id: itemId,
        element: itemElement,
        startX: e.clientX,
        startY: e.clientY,
        initialLeft: parseFloat(itemElement.style.left) || 0,
        initialTop: parseFloat(itemElement.style.top) || 0
    };

    isDragging = false; // Only set to true after minimum movement

    // Disable panzoom during drag
    if (panzoomInstance) {
        panzoomInstance.setOptions({ disablePan: true });
    }

    // Add dragging class
    itemElement.classList.add('dragging');

    // Add global mouse handlers
    document.addEventListener('mousemove', handleItemMouseMove);
    document.addEventListener('mouseup', handleItemMouseUp);
}

/**
 * Handle item mousemove for dragging
 */
function handleItemMouseMove(e) {
    if (!draggedItem) return;

    const deltaX = e.clientX - draggedItem.startX;
    const deltaY = e.clientY - draggedItem.startY;

    // Check if minimum drag distance reached (prevents accidental drags)
    if (!isDragging && (Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3)) {
        isDragging = true;
    }

    if (isDragging) {
        // Apply pan/zoom scale to movement
        const scale = panzoomInstance ? panzoomInstance.getScale() : 1;

        const newLeft = draggedItem.initialLeft + (deltaX / scale);
        const newTop = draggedItem.initialTop + (deltaY / scale);

        draggedItem.element.style.left = `${newLeft}px`;
        draggedItem.element.style.top = `${newTop}px`;
    }
}

/**
 * Handle item mouseup for drag end
 */
function handleItemMouseUp(e) {
    if (!draggedItem) return;

    // Remove global handlers
    document.removeEventListener('mousemove', handleItemMouseMove);
    document.removeEventListener('mouseup', handleItemMouseUp);

    // Re-enable panzoom
    if (panzoomInstance) {
        panzoomInstance.setOptions({ disablePan: false });
    }

    // Remove dragging class
    draggedItem.element.classList.remove('dragging');

    // Only notify if actually dragged
    if (isDragging) {
        const finalLeft = parseFloat(draggedItem.element.style.left) || 0;
        const finalTop = parseFloat(draggedItem.element.style.top) || 0;

        // Notify Blazor of position change
        if (dotnetHelper) {
            dotnetHelper.invokeMethodAsync('OnItemDragEnd', draggedItem.id, finalLeft, finalTop);
        }
    }

    draggedItem = null;
    isDragging = false;
}

/**
 * Handle item click for selection
 */
function handleItemClick(e, itemId) {
    e.stopPropagation();

    // Don't change selection if we just finished dragging
    if (isDragging) {
        return;
    }

    // Multi-select with Ctrl/Cmd
    if (e.ctrlKey || e.metaKey) {
        if (selectedItemIds.has(itemId)) {
            selectedItemIds.delete(itemId);
        } else {
            selectedItemIds.add(itemId);
        }
    } else {
        // Single select
        selectedItemIds.clear();
        selectedItemIds.add(itemId);
    }

    updateSelectionVisuals();
    notifySelectionChanged();
}

/**
 * Handle canvas click for deselection
 */
function handleCanvasClick(e) {
    // Only deselect if clicking directly on canvas, not on items
    if (e.target === canvasElement) {
        selectedItemIds.clear();
        updateSelectionVisuals();
        notifySelectionChanged();
    }
}

/**
 * Update visual state of selected items
 */
function updateSelectionVisuals() {
    itemElements.forEach((element, itemId) => {
        if (selectedItemIds.has(itemId)) {
            element.classList.add('selected');
        } else {
            element.classList.remove('selected');
        }
    });
}

/**
 * Notify Blazor of selection changes
 */
function notifySelectionChanged() {
    if (dotnetHelper) {
        const selectedArray = Array.from(selectedItemIds);
        dotnetHelper.invokeMethodAsync('OnSelectionChanged', selectedArray);
    }
}

/**
 * Get current selection
 */
export function getSelection() {
    return Array.from(selectedItemIds);
}

/**
 * Set selection programmatically
 */
export function setSelection(itemIds) {
    selectedItemIds.clear();
    itemIds.forEach(id => selectedItemIds.add(id));
    updateSelectionVisuals();
}

/**
 * Clear selection
 */
export function clearSelection() {
    selectedItemIds.clear();
    updateSelectionVisuals();
}

/**
 * Get current pan/zoom state
 */
export function getPanZoomState() {
    if (panzoomInstance) {
        const pan = panzoomInstance.getPan();
        const scale = panzoomInstance.getScale();
        return { x: pan.x, y: pan.y, scale: scale };
    }
    return { x: 0, y: 0, scale: 1 };
}

/**
 * Set pan/zoom state
 */
export function setPanZoomState(x, y, scale) {
    if (panzoomInstance) {
        panzoomInstance.pan(x, y);
        panzoomInstance.zoom(scale);
    }
}

/**
 * Get viewport center in canvas coordinates
 */
export function getViewportCenter() {
    if (!canvasElement || !panzoomInstance) {
        return { x: 0, y: 0 };
    }

    const rect = canvasElement.getBoundingClientRect();
    const pan = panzoomInstance.getPan();
    const scale = panzoomInstance.getScale();

    const centerX = (rect.width / 2 - pan.x) / scale;
    const centerY = (rect.height / 2 - pan.y) / scale;

    return { x: centerX, y: centerY };
}

/**
 * Save viewport state to localStorage
 */
function saveViewport() {
    if (!currentBoardId || !panzoomInstance) return;

    const state = getPanZoomState();
    const key = VIEWPORT_STORAGE_KEY + currentBoardId;

    try {
        localStorage.setItem(key, JSON.stringify(state));
    } catch (e) {
        console.warn('Failed to save viewport state:', e);
    }
}

/**
 * Restore viewport state from localStorage
 */
function restoreViewport() {
    if (!currentBoardId || !panzoomInstance) return;

    const key = VIEWPORT_STORAGE_KEY + currentBoardId;

    try {
        const saved = localStorage.getItem(key);
        if (saved) {
            const state = JSON.parse(saved);
            setPanZoomState(state.x, state.y, state.scale);
        }
    } catch (e) {
        console.warn('Failed to restore viewport state:', e);
    }
}

/**
 * Handle resize mousedown
 */
function handleResizeMouseDown(e, itemElement, itemId, position) {
    e.stopPropagation();
    e.preventDefault();

    resizedItem = {
        id: itemId,
        element: itemElement,
        position: position,
        startX: e.clientX,
        startY: e.clientY,
        initialWidth: itemElement.offsetWidth,
        initialHeight: itemElement.offsetHeight,
        initialLeft: parseFloat(itemElement.style.left) || 0,
        initialTop: parseFloat(itemElement.style.top) || 0
    };

    isResizing = false; // Only set to true after minimum movement

    // Disable panzoom during resize
    if (panzoomInstance) {
        panzoomInstance.setOptions({ disablePan: true });
    }

    // Add resizing class
    itemElement.classList.add('resizing');

    // Add global mouse handlers
    document.addEventListener('mousemove', handleResizeMouseMove);
    document.addEventListener('mouseup', handleResizeMouseUp);
}

/**
 * Handle resize mousemove
 */
function handleResizeMouseMove(e) {
    if (!resizedItem) return;

    const deltaX = e.clientX - resizedItem.startX;
    const deltaY = e.clientY - resizedItem.startY;

    // Check if minimum resize distance reached
    if (!isResizing && (Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3)) {
        isResizing = true;
    }

    if (isResizing) {
        // Apply pan/zoom scale to movement
        const scale = panzoomInstance ? panzoomInstance.getScale() : 1;
        const scaledDeltaX = deltaX / scale;
        const scaledDeltaY = deltaY / scale;

        let newWidth = resizedItem.initialWidth;
        let newHeight = resizedItem.initialHeight;
        let newLeft = resizedItem.initialLeft;
        let newTop = resizedItem.initialTop;

        // Calculate new dimensions based on handle position
        switch (resizedItem.position) {
            case 'nw':
                newWidth = Math.max(100, resizedItem.initialWidth - scaledDeltaX);
                newHeight = Math.max(100, resizedItem.initialHeight - scaledDeltaY);
                newLeft = resizedItem.initialLeft + (resizedItem.initialWidth - newWidth);
                newTop = resizedItem.initialTop + (resizedItem.initialHeight - newHeight);
                break;
            case 'n':
                newHeight = Math.max(100, resizedItem.initialHeight - scaledDeltaY);
                newTop = resizedItem.initialTop + (resizedItem.initialHeight - newHeight);
                break;
            case 'ne':
                newWidth = Math.max(100, resizedItem.initialWidth + scaledDeltaX);
                newHeight = Math.max(100, resizedItem.initialHeight - scaledDeltaY);
                newTop = resizedItem.initialTop + (resizedItem.initialHeight - newHeight);
                break;
            case 'e':
                newWidth = Math.max(100, resizedItem.initialWidth + scaledDeltaX);
                break;
            case 'se':
                newWidth = Math.max(100, resizedItem.initialWidth + scaledDeltaX);
                newHeight = Math.max(100, resizedItem.initialHeight + scaledDeltaY);
                break;
            case 's':
                newHeight = Math.max(100, resizedItem.initialHeight + scaledDeltaY);
                break;
            case 'sw':
                newWidth = Math.max(100, resizedItem.initialWidth - scaledDeltaX);
                newHeight = Math.max(100, resizedItem.initialHeight + scaledDeltaY);
                newLeft = resizedItem.initialLeft + (resizedItem.initialWidth - newWidth);
                break;
            case 'w':
                newWidth = Math.max(100, resizedItem.initialWidth - scaledDeltaX);
                newLeft = resizedItem.initialLeft + (resizedItem.initialWidth - newWidth);
                break;
        }

        // Apply new dimensions
        resizedItem.element.style.width = `${newWidth}px`;
        resizedItem.element.style.height = `${newHeight}px`;
        resizedItem.element.style.left = `${newLeft}px`;
        resizedItem.element.style.top = `${newTop}px`;
    }
}

/**
 * Handle resize mouseup
 */
function handleResizeMouseUp(e) {
    if (!resizedItem) return;

    // Remove global handlers
    document.removeEventListener('mousemove', handleResizeMouseMove);
    document.removeEventListener('mouseup', handleResizeMouseUp);

    // Re-enable panzoom
    if (panzoomInstance) {
        panzoomInstance.setOptions({ disablePan: false });
    }

    // Remove resizing class
    resizedItem.element.classList.remove('resizing');

    // Only notify if actually resized
    if (isResizing) {
        const finalWidth = resizedItem.element.offsetWidth;
        const finalHeight = resizedItem.element.offsetHeight;
        const finalLeft = parseFloat(resizedItem.element.style.left) || 0;
        const finalTop = parseFloat(resizedItem.element.style.top) || 0;

        // Notify Blazor of size and position change
        if (dotnetHelper) {
            dotnetHelper.invokeMethodAsync('OnItemResized', resizedItem.id, finalWidth, finalHeight, finalLeft, finalTop);
        }
    }

    resizedItem = null;
    isResizing = false;
}

/**
 * Cleanup and destroy the canvas
 */
export function destroy() {
    // Remove event listeners
    if (canvasElement) {
        canvasElement.removeEventListener('click', handleCanvasClick);
    }

    // Destroy panzoom
    if (panzoomInstance) {
        panzoomInstance.destroy();
        panzoomInstance = null;
    }

    // Clear state
    dotnetHelper = null;
    currentBoardId = null;
    canvasElement = null;
    draggedItem = null;
    selectedItemIds.clear();
    itemElements.clear();
}
