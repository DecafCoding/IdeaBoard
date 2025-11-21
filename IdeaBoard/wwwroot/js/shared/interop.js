// General JSInterop utilities

export function logToConsole(message) {
    console.log('[Blazor]', message);
}

export function focusElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
}
