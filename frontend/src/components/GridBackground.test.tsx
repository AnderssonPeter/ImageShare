import { describe, expect, it, vi } from "vitest";
import { randomFolderUrl, randomRootUrl } from "@lib/api/urls";
import { render, waitFor } from "@testing-library/react";
import GridBackground from "@components/GridBackground";

interface Deferred<TValue> {
  promise: Promise<TValue>;
  resolve: (value: TValue) => void;
  reject: (error: unknown) => void;
}

/**
 * `Promise.withResolvers` exists at runtime (Node 22+) but is absent from the
 * ES2023 lib used for type-checking, so reach it through a cast instead of
 * `new Promise` (which the `promise/avoid-new` rule disallows).
 */
function withResolvers<TValue>(): Deferred<TValue> {
  return (
    Promise as unknown as {
      withResolvers: <UValue>() => Deferred<UValue>;
    }
  ).withResolvers<TValue>();
}

interface FetchRecord {
  url: string;
  deferred: Deferred<Response>;
}

interface FetchContext {
  fetchMock: typeof fetch;
  calls: FetchRecord[];
  resolveCall: (index: number, response: Response) => void;
}

/**
 * Replace global `fetch` with a controllable stub. Each call returns a
 * pending promise resolved by calling `resolveCall(index, response)`, so the
 * test can assert the rendered state *before* the preload completes.
 */
function createControllableFetch(): FetchContext {
  const calls: FetchRecord[] = [];
  const fetchMock = vi.fn<(url: string) => Promise<Response>>((url) =>
    recordFetch(url, calls),
  ) as unknown as typeof fetch;
  return {
    fetchMock,
    calls,
    resolveCall: (index, response) => calls[index]?.deferred.resolve(response),
  };
}

function recordFetch(url: string, calls: FetchRecord[]): Promise<Response> {
  const deferred = withResolvers<Response>();
  calls.push({ url, deferred });
  return deferred.promise;
}

interface ObjectUrlStubs {
  create: ReturnType<typeof vi.fn>;
  revoke: ReturnType<typeof vi.fn>;
}

function stubObjectUrls(): ObjectUrlStubs {
  let counter = 0;
  const create = vi.fn<() => string>(() => `blob:${++counter}`);
  const revoke = vi.fn<(url: string) => void>();
  vi.stubGlobal("URL", {
    ...(globalThis.URL as unknown as object),
    createObjectURL: create,
    revokeObjectURL: revoke,
  });
  return { create, revoke };
}

function okResponse(): Response {
  return {
    ok: true,
    status: 200,
    blob: () => Promise.resolve(new Blob(["x"], { type: "image/png" })),
  } as unknown as Response;
}

function notFoundResponse(): Response {
  return { ok: false, status: 404, blob: () => Promise.resolve(new Blob()) } as unknown as Response;
}

/**
 * The jsdom `Image` has no `decode()` method. Provide a minimal stub so the
 * component's `await image.decode()` resolves immediately. `vi.stubGlobal`
 * auto-restores between tests.
 */
function stubImage(): void {
  vi.stubGlobal(
    "Image",
    class {
      src = "";
      decode(): Promise<void> {
        return Promise.resolve();
      }
    },
  );
}

/** Return the background-image of the currently visible (opacity 1) image layer. */
function visibleBackgroundOf(container: HTMLElement): string {
  for (const child of container.children) {
    const element = child as HTMLElement;
    if (element.style.backgroundImage && element.style.opacity === "1") {
      return element.style.backgroundImage.replaceAll('"', "");
    }
  }
  return "";
}

/** Common arrange: controllable fetch, object-URL stubs, and the rendered component. */
function setupContainer(props: {
  path?: string;
}): FetchContext & ObjectUrlStubs & ReturnType<typeof render> {
  const fetchContext = createControllableFetch();
  vi.stubGlobal("fetch", fetchContext.fetchMock);
  const urls = stubObjectUrls();
  stubImage();
  const result = render(<GridBackground path={props.path} />);
  return { ...fetchContext, ...urls, ...result };
}

describe("gridBackground initial load", () => {
  it("renders no background image until the random image is preloaded", async () => {
    expect.hasAssertions();
    // Arrange + Act
    const { container, calls, resolveCall } = setupContainer({});
    // Assert — before the preload resolves, nothing is shown (no flash of broken bg)
    expect(visibleBackgroundOf(container)).toBe("");
    resolveCall(0, okResponse());
    await waitFor(() => {
      expect(visibleBackgroundOf(container)).not.toBe("");
    });
    expect(calls[0]?.url).toBe(randomRootUrl(false));
  }, 2000);

  it("requests the recursive random folder image when a path is given", async () => {
    expect.hasAssertions();
    // Arrange + Act
    const { calls, resolveCall } = setupContainer({ path: "photos/2024" });
    resolveCall(0, okResponse());
    // Assert
    await waitFor(() => {
      expect(calls[0]?.url).toBe(randomFolderUrl("photos/2024", false, true));
    });
  }, 2000);
});

describe("gridBackground folder change", () => {
  it("keeps the previous image visible while the next one preloads, then swaps", async () => {
    expect.hasAssertions();
    // Arrange + Act — initial load
    const { container, rerender, resolveCall, create, revoke } = setupContainer({ path: "a" });
    resolveCall(0, okResponse());
    await waitFor(() => {
      expect(visibleBackgroundOf(container)).toBe("url(blob:1)");
    });
    // Act — navigate to a new folder (second fetch pending)
    rerender(<GridBackground path="b" />);
    // Assert — old image still shown until the new preload completes
    expect(visibleBackgroundOf(container)).toBe("url(blob:1)");
    resolveCall(1, okResponse());
    await waitFor(() => {
      expect(visibleBackgroundOf(container)).toBe("url(blob:2)");
    });
    // Assert — previous object URL was revoked after the fade-out completed
    await waitFor(() => {
      expect(revoke).toHaveBeenCalledWith("blob:1");
    });
    expect(create).toHaveBeenCalledTimes(2);
  }, 3000);

  it("leaves the previous background in place when the next preload fails", async () => {
    expect.hasAssertions();
    // Arrange + Act — initial load
    const { container, rerender, calls, resolveCall } = setupContainer({ path: "a" });
    resolveCall(0, okResponse());
    await waitFor(() => {
      expect(visibleBackgroundOf(container)).not.toBe("");
    });
    const before = visibleBackgroundOf(container);
    // Act — navigate to a folder whose random image returns 404
    rerender(<GridBackground path="b" />);
    resolveCall(1, notFoundResponse());
    // Assert — both fetches fired, but the old background remains
    await waitFor(() => {
      expect(calls).toHaveLength(2);
    });
    expect(visibleBackgroundOf(container)).toBe(before);
  }, 3000);
});

describe("gridBackground unmount", () => {
  it("revokes the active object URL on unmount", async () => {
    expect.hasAssertions();
    // Arrange + Act
    const { container, resolveCall, revoke, unmount } = setupContainer({});
    resolveCall(0, okResponse());
    await waitFor(() => {
      expect(visibleBackgroundOf(container)).not.toBe("");
    });
    unmount();
    // Assert
    expect(revoke).toHaveBeenCalledWith("blob:1");
  }, 2000);
});
