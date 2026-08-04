/**
 * ThemeProvider — single source of truth for the active theme.
 *
 * Wraps the app once (mounted in the root route layout) and exposes the
 * resolved theme plus mutators through React context. Mounting the provider
 * once — rather than each consumer calling a hook — guarantees a single
 * `matchMedia` listener and a single piece of state that cannot drift.
 *
 * Behaviour:
 *  - Applies the resolved theme (override > system > default) on mount.
 *  - Subscribes to live system colour-scheme changes; when no localStorage
 *    override is present the effective theme follows the system, otherwise
 *    the override wins and system changes are ignored.
 *  - `setTheme` persists an explicit override and applies it.
 *  - `clearOverride` drops the override and re-follows the system preference.
 */
import {
  type ReactNode,
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  type Theme,
  applyTheme,
  clearStoredTheme,
  hasThemeOverride,
  onSystemThemeChange,
  resolveTheme,
  storeTheme,
} from "@lib/theme";

export interface ThemeContextValue {
  /** The currently effective theme. */
  theme: Theme;
  /** Persist an explicit override and apply it immediately. */
  setTheme: (theme: Theme) => void;
  /** Drop the override and re-follow the system preference. */
  clearOverride: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [currentTheme, setCurrentTheme] = useState<Theme>(() => resolveTheme());

  // Apply the current theme to <html> whenever it changes.
  useEffect(() => {
    applyTheme(currentTheme);
  }, [currentTheme]);

  // Live system listener — only follows the system when no override is set.
  useEffect(
    () =>
      onSystemThemeChange((systemTheme) => {
        if (!hasThemeOverride()) {
          setCurrentTheme(systemTheme);
        }
      }),
    [],
  );

  const setTheme = useCallback((next: Theme) => {
    storeTheme(next);
    setCurrentTheme(next);
  }, []);

  const clearOverride = useCallback(() => {
    clearStoredTheme();
    setCurrentTheme(resolveTheme());
  }, []);

  const value: ThemeContextValue = useMemo<ThemeContextValue>(
    () => ({ theme: currentTheme, setTheme, clearOverride }),
    [currentTheme, setTheme, clearOverride],
  );
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/** Consume the active theme. Throws if rendered outside a `ThemeProvider`. */
export function useThemeContext(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error("useThemeContext must be used within a ThemeProvider");
  }
  return context;
}
