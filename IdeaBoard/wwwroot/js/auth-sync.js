/**
 * Cross-tab authentication synchronization
 * Listens for localStorage changes and notifies Blazor when auth tokens are cleared
 */

window.authSync = {
    dotNetReference: null,

    /**
     * Initialize the auth sync listener
     * @param {any} dotNetRef - Reference to the .NET object to call back
     */
    initialize: function (dotNetRef) {
        this.dotNetReference = dotNetRef;

        // Listen for storage events (triggered when localStorage changes in another tab)
        window.addEventListener('storage', this.handleStorageChange.bind(this));

        console.log('Auth sync initialized');
    },

    /**
     * Handle localStorage changes from other tabs
     * @param {StorageEvent} event - The storage event
     */
    handleStorageChange: function (event) {
        // Check if auth tokens were changed
        if (event.key === 'auth_access_token' || event.key === 'auth_refresh_token') {
            // If token was removed (newValue is null), user logged out in another tab
            if (event.newValue === null && this.dotNetReference) {
                console.log('Auth token removed in another tab - syncing logout');
                this.dotNetReference.invokeMethodAsync('OnAuthStateChangedInOtherTab');
            }
            // If token was added (oldValue was null), user logged in in another tab
            else if (event.oldValue === null && event.newValue !== null && this.dotNetReference) {
                console.log('Auth token added in another tab - syncing login');
                this.dotNetReference.invokeMethodAsync('OnAuthStateChangedInOtherTab');
            }
        }
    },

    /**
     * Cleanup - remove event listener
     */
    dispose: function () {
        window.removeEventListener('storage', this.handleStorageChange.bind(this));
        this.dotNetReference = null;
        console.log('Auth sync disposed');
    }
};
