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

vi.mock(import("@lib/api/generated"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    getContent: mockGetContent as never,
    getContentByPath: mockGetContentByPath as never,
  };
});

interface PageData {
  items: FolderEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
}

function sdkResponse(pageData: PageData) {
  return {
    data: pageData,
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
  it("calls getContent with lowercase page/pageSize for root listing", async () => {
    expect.assertions(2);
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContent.mockResolvedValueOnce(
      sdkResponse({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
    );

    // Act
    renderContentHook();
    await waitFor(() => {
      expect(mockGetContent).toHaveBeenCalledTimes(1);
    });

    // Assert
    expect(mockGetContent).toHaveBeenCalledWith(
      expect.objectContaining({ query: { page: 1, pageSize: 50 } }),
    );
  }, 2000);
});

describe("useFolderContent subfolder routing", () => {
  it("calls getContentByPath with lowercase page/pageSize for subfolder", async () => {
    expect.assertions(2);
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContentByPath.mockResolvedValueOnce(
      sdkResponse({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
    );

    // Act
    renderContentHook("photos/2024");
    await waitFor(() => {
      expect(mockGetContentByPath).toHaveBeenCalledTimes(1);
    });

    // Assert
    expect(mockGetContentByPath).toHaveBeenCalledWith(
      expect.objectContaining({ path: { path: "photos/2024" }, query: { page: 1, pageSize: 50 } }),
    );
  }, 2000);
});

describe("useFolderContent pagination has more", () => {
  it("reports hasNextPage when more items remain", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContent.mockResolvedValueOnce(
      sdkResponse({
        items: [{ name: "a", path: "a", type: "Folder" as const }],
        page: 1,
        pageSize: 50,
        totalCount: 100,
      }),
    );

    // Act
    const { result } = renderContentHook();
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    // Assert
    expect(result.current.hasNextPage).toBe(true);
  }, 2000);
});

describe("useFolderContent pagination last page", () => {
  it("reports no hasNextPage when on the last page", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetContent.mockReset();
    mockGetContentByPath.mockReset();
    mockGetContent.mockResolvedValueOnce(
      sdkResponse({ items: [], page: 1, pageSize: 50, totalCount: 30 }),
    );

    // Act
    const { result } = renderContentHook();
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    // Assert
    expect(result.current.hasNextPage).toBe(false);
  }, 2000);
});
