import {
  DEFAULT_THEME,
  type Theme,
  applyTheme,
  clearStoredTheme,
  onSystemThemeChange,
  resolveTheme,
  storeTheme,
} from "@lib/theme";
import { describe, expect, it, vi } from "vitest";

type StorageReturn = string | undefined;

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

interface MatchMediaResult {
  matches: boolean;
  media: string;
  addEventListener: ReturnType<typeof vi.fn>;
  removeEventListener: ReturnType<typeof vi.fn>;
}

function createMatchMedia(isDark: boolean): ReturnType<typeof vi.fn> {
  return vi.fn<(query: string) => MatchMediaResult>().mockImplementation((query: string) => ({
    matches: isDark,
    media: query,
    addEventListener: vi.fn<() => void>(),
    removeEventListener: vi.fn<() => void>(),
  }));
}

function setupEnvironment(systemDark: boolean) {
  vi.stubGlobal("localStorage", createLocalStorage());
  vi.stubGlobal("matchMedia", createMatchMedia(systemDark));
}

describe("resolveTheme detection", () => {
  it(
    "returns dark when matchMedia is unavailable",
    () => {
      expect.hasAssertions();
      // Arrange — stub localStorage but leave matchMedia unset
      vi.stubGlobal("localStorage", createLocalStorage());
      vi.stubGlobal("matchMedia", void 0);

      // Act + Assert
      expect(resolveTheme()).toBe(DEFAULT_THEME);
    },
    1000,
  );

  it(
    "returns dark when system prefers dark and no override",
    () => {
      expect.hasAssertions();
      // Arrange
      setupEnvironment(true);

      // Act + Assert
      expect(resolveTheme()).toBe("dark");
    },
    1000,
  );

  it(
    "returns light when system prefers light and no override",
    () => {
      expect.hasAssertions();
      // Arrange
      setupEnvironment(false);

      // Act + Assert
      expect(resolveTheme()).toBe("light");
    },
    1000,
  );
});

describe("resolveTheme localStorage override", () => {
  it(
    "returns the stored override instead of the system preference",
    () => {
      expect.hasAssertions();
      // Arrange
      setupEnvironment(false);
      storeTheme("dark");

      // Act + Assert
      expect(resolveTheme()).toBe("dark");
    },
    1000,
  );

  it(
    "falls back to system after clearing the override",
    () => {
      expect.hasAssertions();
      // Arrange
      setupEnvironment(false);
      storeTheme("dark");
      clearStoredTheme();

      // Act + Assert
      expect(resolveTheme()).toBe("light");
    },
    1000,
  );
});

describe("applyTheme toggling dark", () => {
  it(
    "adds the dark class to <html> for dark theme",
    () => {
      expect.hasAssertions();
      // Arrange
      document.documentElement.classList.remove("dark");

      // Act
      applyTheme("dark");

      // Assert
      expect(document.documentElement.classList.contains("dark")).toBe(true);
    },
    1000,
  );
});

describe("applyTheme toggling light", () => {
  it(
    "removes the dark class from <html> for light theme",
    () => {
      expect.hasAssertions();
      // Arrange
      document.documentElement.classList.add("dark");

      // Act
      applyTheme("light");

      // Assert
      expect(document.documentElement.classList.contains("dark")).toBe(false);
    },
    1000,
  );
});

describe("onSystemThemeChange subscription", () => {
  function setupChangeMediaMock() {
    type ChangeListener = (event: MediaQueryListEvent) => void;
    const listeners: ChangeListener[] = [];
    const addEventListenerMock = vi.fn<(event: string, listener: ChangeListener) => void>();
    addEventListenerMock.mockImplementation((event, listener) => {
      if (event === "change") {
        listeners.push(listener);
      }
    });
    vi.stubGlobal(
      "matchMedia",
      vi.fn<(query: string) => MatchMediaResult>().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        addEventListener: addEventListenerMock,
        removeEventListener: vi.fn<() => void>(),
      })),
    );
    return listeners;
  }

  it(
    "calls the handler when the system theme changes",
    () => {
      expect.hasAssertions();
      // Arrange
      const listeners = setupChangeMediaMock();
      const handler = vi.fn<(theme: Theme) => void>();

      // Act
      onSystemThemeChange(handler);
      for (const listener of listeners) {
        listener({ matches: true } as MediaQueryListEvent);
      }

      // Assert
      expect(handler).toHaveBeenCalledWith("dark");
    },
    1000,
  );
});
