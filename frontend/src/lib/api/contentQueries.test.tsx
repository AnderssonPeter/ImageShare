import { type FolderEntry, type getContent, type getContentByPath } from "@lib/api/generated";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { type ReactNode } from "react";
import { useFolderContent } from "@lib/api/contentQueries";

const { mockGetContent, mockGetContentByPath } = vi.hoisted(() => ({
  mockGetContent: vi.fn<typeof getContent>(),
  mockGetContentByPath: vi.fn<typeof getContentByPath>(),
}));

vi.mock(import("@lib/api/generated/sdk.gen"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    getContent: mockGetContent as never,
    getContentByPath: mockGetContentByPath as never,
  };
});

function sdkResponse(items: FolderEntry[]) {
  return {
    data: items,
    request: new Request("http://localhost/api/content"),
    response: new Response(),
  } as never;
}

function renderContentHook(path?: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  return renderHook(() => useFolderContent(path), { wrapper: Wrapper });
}

describe("useFolderContent root routing", () => {
  it("calls getContent for root listing", async () => {
    expect.assertions(2);
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContent.mockResolvedValueOnce(sdkResponse([]));

    // Act
    renderContentHook();
    await waitFor(() => {
      expect(mockGetContent).toHaveBeenCalledTimes(1);
    });

    // Assert
    expect(mockGetContent).toHaveBeenCalledTimes(1);
  }, 2000);
});

describe("useFolderContent subfolder routing", () => {
  it("calls getContentByPath for subfolder", async () => {
    expect.assertions(2);
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContentByPath.mockResolvedValueOnce(sdkResponse([]));

    // Act
    renderContentHook("photos/2024");
    await waitFor(() => {
      expect(mockGetContentByPath).toHaveBeenCalledTimes(1);
    });

    // Assert
    expect(mockGetContentByPath).toHaveBeenCalledWith(
      expect.objectContaining({ path: { path: "photos/2024" } }),
    );
  }, 2000);
});

describe("useFolderContent returns entries", () => {
  it("returns the folder entries array directly", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    const entries: FolderEntry[] = [
      { name: "a", path: "a", type: "Folder" },
      { name: "b", path: "b", type: "File" },
    ];
    mockGetContent.mockResolvedValueOnce(sdkResponse(entries));

    // Act
    const { result } = renderContentHook();
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    // Assert
    expect(result.current.data).toStrictEqual(entries);
  }, 2000);
});
