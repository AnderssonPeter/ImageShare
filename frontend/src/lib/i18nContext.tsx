/**
 * LanguageProvider — single source of truth for the active language.
 *
 * Wraps the app once (mounted in the root route layout) and resolves the
 * initial language (override > navigator > default), then keeps the shared
 * `i18n` instance in sync when the user switches. Components consume
 * translated strings through `useTranslation()` (which re-renders on
 * `languageChanged`); this provider only owns the preference + switching so
 * the `LanguageToggle` has a stable `language` value and `setLanguage`.
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
import { type Language, i18n, resolveLanguage, storeLanguage } from "@lib/i18n";

export interface LanguageContextValue {
  /** The currently effective language. */
  language: Language;
  /** Persist an explicit choice, switch `i18n`, and apply it immediately. */
  setLanguage: (language: Language) => void;
}

const LanguageContext = createContext<LanguageContextValue | undefined>(undefined);

export function LanguageProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [currentLanguage, setCurrentLanguage] = useState<Language>(() => resolveLanguage());

  useEffect(() => {
    void i18n.changeLanguage(currentLanguage);
  }, [currentLanguage]);

  useEffect(() => {
    function handleLanguageChanged(changed: string): void {
      if (changed === "en" || changed === "sv") {
        setCurrentLanguage(changed);
      }
    }
    i18n.on("languageChanged", handleLanguageChanged);
    return () => {
      i18n.off("languageChanged", handleLanguageChanged);
    };
  }, []);

  const setLanguage = useCallback((next: Language) => {
    storeLanguage(next);
    setCurrentLanguage(next);
  }, []);

  const value = useMemo<LanguageContextValue>(
    () => ({ language: currentLanguage, setLanguage }),
    [currentLanguage, setLanguage],
  );
  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

/** Consume the active language. Throws if rendered outside a `LanguageProvider`. */
export function useLanguageContext(): LanguageContextValue {
  const context = useContext(LanguageContext);
  if (context === undefined) {
    throw new Error("useLanguageContext must be used within a LanguageProvider");
  }
  return context;
}
