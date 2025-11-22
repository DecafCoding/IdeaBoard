// wwwroot/js/theme.js
// Theme management for Bootstrap dark mode support.
// Uses window object pattern for Blazor compatibility.

(function () {
    'use strict';

    const THEME_KEY = 'ideaboard-theme';
    const THEMES = {
        LIGHT: 'light',
        DARK: 'dark',
        AUTO: 'auto'
    };

    /**
     * Applies the theme to the document by setting data-bs-theme attribute
     */
    function applyTheme(theme) {
        const htmlElement = document.documentElement;

        if (theme === THEMES.AUTO) {
            // Remove the attribute to let Bootstrap use system preference
            htmlElement.removeAttribute('data-bs-theme');

            // Add a class to indicate auto mode for CSS targeting
            htmlElement.classList.add('theme-auto');
            htmlElement.classList.remove('theme-light', 'theme-dark');
        } else {
            // Set explicit theme
            htmlElement.setAttribute('data-bs-theme', theme);

            // Add helper classes for CSS targeting
            htmlElement.classList.remove('theme-auto');
            htmlElement.classList.add(`theme-${theme}`);
            htmlElement.classList.remove(`theme-${theme === 'light' ? 'dark' : 'light'}`);
        }
    }

    /**
     * Gets the current theme from localStorage or defaults to 'auto'
     */
    function getTheme() {
        try {
            return localStorage.getItem(THEME_KEY) || THEMES.AUTO;
        } catch {
            return THEMES.AUTO;
        }
    }

    /**
     * Sets the theme and applies it to the document
     */
    function setTheme(theme) {
        try {
            // Validate theme value
            if (!Object.values(THEMES).includes(theme)) {
                theme = THEMES.AUTO;
            }

            // Save to localStorage
            localStorage.setItem(THEME_KEY, theme);

            // Apply theme to document
            applyTheme(theme);

            // Dispatch custom event for other components to listen to
            window.dispatchEvent(new CustomEvent('themeChanged', { detail: theme }));
        } catch (error) {
            console.warn('Failed to set theme:', error);
        }
    }

    /**
     * Gets the effective theme (resolves 'auto' to actual theme)
     */
    function getEffectiveTheme() {
        const theme = getTheme();
        if (theme === THEMES.AUTO) {
            return window.matchMedia('(prefers-color-scheme: dark)').matches ? THEMES.DARK : THEMES.LIGHT;
        }
        return theme;
    }

    /**
     * Initialize theme on page load
     */
    function initializeTheme() {
        const savedTheme = getTheme();
        applyTheme(savedTheme);

        // Listen for system theme changes when in auto mode
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
            if (getTheme() === THEMES.AUTO) {
                applyTheme(THEMES.AUTO);
            }
        });

        return savedTheme;
    }

    // Expose functions globally for Blazor JSInterop
    window.IdeaBoardTheme = {
        getTheme: getTheme,
        setTheme: setTheme,
        getEffectiveTheme: getEffectiveTheme,
        initializeTheme: initializeTheme
    };

    // Critical: Apply theme immediately to prevent FOUC
    if (typeof document !== 'undefined') {
        const savedTheme = getTheme();
        applyTheme(savedTheme);
    }

    // Auto-initialize when DOM is ready
    if (typeof document !== 'undefined') {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeTheme);
        } else {
            initializeTheme();
        }
    }
})();
