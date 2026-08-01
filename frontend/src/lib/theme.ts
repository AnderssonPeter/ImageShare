/**
 * Theme detection — determines whether to use dark or light mode.
 *
 * Detection order (per TODO.md):
 *  1. A localStorage override (key: `theme`) if the user has explicitly chosen.
 *  2. The system `prefers-color-scheme: dark` media query.
 *  3. Fallback to dark.
 *
 * The chosen theme is applied by toggling the `dark` class on `<html>` (the
 * CSS uses `@custom-variant dark (&:is(.dark *))` in index.css).
 */

export type Theme = 'light' | 'dark'

/** `localStorage` key for the user's explicit theme override. */
const THEME_STORAGE_KEY = 'theme'

/** Default theme when no override and no system preference is available. */
export const DEFAULT_THEME: Theme = 'dark'

/** Read the explicit localStorage override, if any. */
function getStoredTheme(): Theme | undefined {
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') {
    return stored
  }
  return undefined
}

/** Read the system colour-scheme preference via matchMedia. */
function getSystemTheme(): Theme {
  if (typeof globalThis === 'undefined' || !globalThis.matchMedia) {
    return DEFAULT_THEME
  }
  return globalThis.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

/** Resolve the effective theme: explicit override > system > default. */
export function resolveTheme(): Theme {
  return getStoredTheme() ?? getSystemTheme()
}

/** Apply a theme to the document by toggling the `dark` class on `<html>`. */
export function applyTheme(theme: Theme): void {
  document.documentElement.classList.toggle('dark', theme === 'dark')
}

/** Persist an explicit theme choice to localStorage. */
export function storeTheme(theme: Theme): void {
  localStorage.setItem(THEME_STORAGE_KEY, theme)
}

/** Remove the explicit override so the system preference takes over. */
export function clearStoredTheme(): void {
  localStorage.removeItem(THEME_STORAGE_KEY)
}

/** Whether the user has set an explicit theme override in localStorage. */
export function hasThemeOverride(): boolean {
  return getStoredTheme() !== undefined
}

/** Subscribe to system colour-scheme changes. Returns an unsubscribe function. */
export function onSystemThemeChange(onChange: (theme: Theme) => void): () => void {
  if (typeof globalThis === 'undefined' || !globalThis.matchMedia) {
    return () => {}
  }
  const mediaQuery = globalThis.matchMedia('(prefers-color-scheme: dark)')
  function listener(event: MediaQueryListEvent) {
    onChange(event.matches ? 'dark' : 'light')
  }
  mediaQuery.addEventListener('change', listener)
  return () => mediaQuery.removeEventListener('change', listener)
}
