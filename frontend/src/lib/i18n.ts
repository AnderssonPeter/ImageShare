/**
 * I18n — language detection and the i18next instance.
 *
 * Detection order (mirrors `theme.ts`):
 *  1. A localStorage override (key: `language`) if explicitly chosen.
 *  2. The browser's `navigator.language` (matched against supported languages).
 *  3. Fallback to `DEFAULT_LANGUAGE` (`"en"`).
 *
 * The configured `i18n` instance is shared with `react-i18next` so `useTranslation()`
 * reads from it. A `LanguageProvider` (see `i18nContext.tsx`) drives the live
 * language — initial resolution, switching, and persistence — while components
 * consume translations through `useTranslation()` (re-exported here for convenience).
 *
 * Translation keys are type-checked at compile time via the module augmentation in
 * `i18next.d.ts` (`strictKeyChecks: true`). Callers must use `useTranslation()` (in
 * React) or `translate()` (outside React) — never `i18n.t()` directly.
 */
import i18next, { getFixedT, use as registerPlugin } from "i18next";
import { initReactI18next, type useTranslation } from "react-i18next";
import en from "@lib/i18n/locales/en";
import sv from "@lib/i18n/locales/sv";

export type Language = "en" | "sv";

/** The configured i18next instance — exported for language management only (not `t()`). */
export { default as i18n } from "i18next";
/** React hook for translated strings — re-exported so callers import one module. */
export { useTranslation } from "react-i18next";
/** Shorthand for the `t` function returned by `useTranslation()`. */
export type Translate = ReturnType<typeof useTranslation>["t"];

/** `localStorage` key for the user's explicit language override. */
const LANGUAGE_STORAGE_KEY = "language";

/** Default language when no override and no navigator match is available. */
export const DEFAULT_LANGUAGE: Language = "en";

/** Languages the app ships translations for. */
export const SUPPORTED_LANGUAGES: readonly Language[] = ["en", "sv"];

/**
 * Returns a typed `TFunction` bound to the current language, for use outside React
 * (e.g. the global query error handler). Callers must invoke this at translation
 * time — not cache the result — so language changes are picked up.
 */
export function translate(): Translate {
  return getFixedT(i18next.language);
}

function isLanguage(value: string | null): value is Language {
  return value === "en" || value === "sv";
}

/** Safely reach `localStorage` (absent in Node/SSR and some test setups). */
function getStorage(): Storage | undefined {
  try {
    return typeof globalThis === "undefined" ? undefined : globalThis.localStorage;
  } catch {
    return undefined;
  }
}

/** Read the explicit localStorage override, if any. */
function getStoredLanguage(): Language | undefined {
  const storage = getStorage();
  if (storage === undefined) {
    return undefined;
  }
  const stored = storage.getItem(LANGUAGE_STORAGE_KEY);
  return isLanguage(stored) ? stored : undefined;
}

/** Match the browser language against a supported language (by primary subtag). */
function matchNavigatorLanguage(): Language | undefined {
  if (typeof globalThis === "undefined" || !globalThis.navigator) {
    return undefined;
  }
  const [primary] = globalThis.navigator.language.split("-");
  return isLanguage(primary) ? primary : undefined;
}

/** Resolve the effective language: explicit override > navigator > default. */
export function resolveLanguage(): Language {
  return getStoredLanguage() ?? matchNavigatorLanguage() ?? DEFAULT_LANGUAGE;
}

/** Persist an explicit language choice to localStorage. */
export function storeLanguage(language: Language): void {
  const storage = getStorage();
  storage?.setItem(LANGUAGE_STORAGE_KEY, language);
}

/** Remove the explicit override so the navigator preference takes over. */
export function clearStoredLanguage(): void {
  const storage = getStorage();
  storage?.removeItem(LANGUAGE_STORAGE_KEY);
}

/** Whether the user has set an explicit language override in localStorage. */
export function hasLanguageOverride(): boolean {
  return getStoredLanguage() !== undefined;
}

void registerPlugin(initReactI18next).init({
  resources: {
    en: { translation: en },
    sv: { translation: sv },
  },
  lng: DEFAULT_LANGUAGE,
  fallbackLng: DEFAULT_LANGUAGE,
  supportedLngs: [...SUPPORTED_LANGUAGES],
  interpolation: { escapeValue: false },
  initAsync: false,
  react: { useSuspense: false },
  partialBundledLanguages: true,
});
