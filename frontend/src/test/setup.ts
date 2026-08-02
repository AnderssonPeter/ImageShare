import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";

/**
 * Provide no-op stubs for browser APIs jsdom does not implement. The
 * Embla-based carousel (and other components) call these during mount, so
 * without the stubs they throw and unmount the tree. Theme tests override
 * `matchMedia` with a richer, controllable stub via `setupThemeEnvironment`.
 */
vi.stubGlobal(
  "matchMedia",
  (query: string): MediaQueryList =>
    ({
      matches: false,
      media: query,
      onchange: undefined,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }) as unknown as MediaQueryList,
);

function makeObserverStub(): IntersectionObserver {
  return {
    observe: () => {},
    unobserve: () => {},
    disconnect: () => {},
    takeRecords: () => [],
    root: undefined,
    rootMargin: "",
    thresholds: [],
  } as unknown as IntersectionObserver;
}

vi.stubGlobal("IntersectionObserver", function IntersectionObserverStub(): IntersectionObserver {
  return makeObserverStub();
} as unknown as typeof IntersectionObserver);

function makeResizeObserverStub(): ResizeObserver {
  return {
    observe: () => {},
    unobserve: () => {},
    disconnect: () => {},
  } as unknown as ResizeObserver;
}

vi.stubGlobal("ResizeObserver", function ResizeObserverStub(): ResizeObserver {
  return makeResizeObserverStub();
} as unknown as typeof ResizeObserver);
