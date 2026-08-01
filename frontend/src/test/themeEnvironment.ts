import { vi } from "vitest";
/**
 * Shared theme test environment — stubs `localStorage` and `matchMedia` so
 * theme-dependent components can be exercised deterministically under jsdom.
 *
 * Call `setupThemeEnvironment(systemDark)` inside a test to install a fresh
 * in-memory localStorage and a controllable `prefers-color-scheme` media
 * query. The returned control lets the test flip the system theme and
 * inspect listener wiring.
 */

type StorageReturn = string | undefined;

interface MatchMediaResult {
  matches: boolean;
  media: string;
  addEventListener: ReturnType<typeof vi.fn>;
  removeEventListener: ReturnType<typeof vi.fn>;
}

type ChangeListener = (event: MediaQueryListEvent) => void;

interface ThemeMediaControl {
  addEventListener: ReturnType<typeof vi.fn>;
  removeEventListener: ReturnType<typeof vi.fn>;
  setSystemTheme: (isDark: boolean) => void;
  listenerCount: () => number;
}

function createLocalStorage() {
  const store = new Map<string, string>();
  return {
    getItem: vi.fn<(key: string) => StorageReturn>((key) => store.get(key)),
    setItem: vi.fn<(key: string, value: string) => void>((key, value) => {
      store.set(key, value);
    }),
    removeItem: vi.fn<(key: string) => void>((key) => {
      store.delete(key);
    }),
    clear: vi.fn<() => void>(() => {
      store.clear();
    }),
    key: vi.fn<(index: number) => StorageReturn>((index) => [...store.keys()][index]),
    get length() {
      return store.size;
    },
  };
}

function createChangeableMatchMedia(systemDark: boolean) {
  const listeners: ChangeListener[] = [];
  let matches = systemDark;
  const addEventListener = vi
    .fn<(event: string, listener: ChangeListener) => void>()
    .mockImplementation((event, listener) => {
      if (event === "change") {
        listeners.push(listener);
      }
    });
  const removeEventListener = vi
    .fn<(event: string, listener: ChangeListener) => void>()
    .mockImplementation((event, listener) => {
      if (event === "change") {
        const index = listeners.indexOf(listener);
        if (index !== -1) {
          listeners.splice(index, 1);
        }
      }
    });
  vi.stubGlobal(
    "matchMedia",
    vi.fn<(query: string) => MatchMediaResult>().mockImplementation((query: string) => ({
      matches,
      media: query,
      addEventListener,
      removeEventListener,
    })),
  );
  return {
    addEventListener,
    removeEventListener,
    setSystemTheme(isDark: boolean) {
      matches = isDark;
      for (const listener of listeners) {
        listener({ matches } as MediaQueryListEvent);
      }
    },
    listenerCount: () => listeners.length,
  };
}

/**
 * Stub `localStorage` and `matchMedia` for a test. Returns a control to flip
 * the system theme and inspect listener wiring.
 */
export default function setupThemeEnvironment(systemDark: boolean): ThemeMediaControl {
  vi.stubGlobal("localStorage", createLocalStorage());
  return createChangeableMatchMedia(systemDark);
}
