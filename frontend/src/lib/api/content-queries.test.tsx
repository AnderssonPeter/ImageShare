import { type FolderEntry, type getContent, type getContentPath } from "./generated/imageShare";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { type ReactNode } from "react";
import { useFolderContent } from "./content-queries";

const { mockGetContent, mockGetContentPath } = vi.hoisted(() => ({
  mockGetContent: vi.fn<typeof getContent>(),
  mockGetContentPath: vi.fn<typeof getContentPath>(),
}));

vi.mock(import("./generated/imageShare"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    getContent: mockGetContent,
    getContentPath: mockGetContentPath,
  };
});

interface PageData {
  items: FolderEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
}

function pageResponse(pageData: PageData) {
  return {
    status: 200 as const,
    data: pageData,
    headers: new Headers(),
  };
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
  it(
    "calls getContent with lowercase page/pageSize for root listing",
    async () => {
      expect.assertions(2);
      // Arrange
      mockGetContent.mockReset();
      mockGetContentPath.mockReset();
      mockGetContent.mockResolvedValueOnce(
        pageResponse({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
      );

      // Act
      renderContentHook();
      await waitFor(() => {
        expect(mockGetContent).toHaveBeenCalledTimes(1);
      });

      // Assert
      expect(mockGetContent).toHaveBeenCalledWith(
        { page: 1, pageSize: 50 },
        expect.any(Object),
      );
    },
    2000,
  );
});

describe("useFolderContent subfolder routing", () => {
  it(
    "calls getContentPath with lowercase page/pageSize for subfolder",
    async () => {
      expect.assertions(2);
      // Arrange
      mockGetContent.mockReset();
      mockGetContentPath.mockReset();
      mockGetContentPath.mockResolvedValueOnce(
        pageResponse({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
      );

      // Act
      renderContentHook("photos/2024");
      await waitFor(() => {
        expect(mockGetContentPath).toHaveBeenCalledTimes(1);
      });

      // Assert
      expect(mockGetContentPath).toHaveBeenCalledWith(
        "photos/2024",
        { page: 1, pageSize: 50 },
        expect.any(Object),
      );
    },
    2000,
  );
});

describe("useFolderContent pagination has more", () => {
  it(
    "reports hasNextPage when more items remain",
    async () => {
      expect.hasAssertions();
      // Arrange
      mockGetContent.mockReset();
      mockGetContentPath.mockReset();
      mockGetContent.mockResolvedValueOnce(
        pageResponse({
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
    },
    2000,
  );
});

describe("useFolderContent pagination last page", () => {
  it(
    "reports no hasNextPage when on the last page",
    async () => {
      expect.hasAssertions();
      // Arrange
      mockGetContent.mockReset();
      mockGetContentPath.mockReset();
      mockGetContent.mockResolvedValueOnce(
        pageResponse({ items: [], page: 1, pageSize: 50, totalCount: 30 }),
      );

      // Act
      const { result } = renderContentHook();
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Assert
      expect(result.current.hasNextPage).toBe(false);
    },
    2000,
  );
});
