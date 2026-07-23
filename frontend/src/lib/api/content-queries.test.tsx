import { type ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useFolderContent } from "./content-queries";
import type { FolderEntry } from "./generated/imageShare";

const { mockGetContent, mockGetContentPath } = vi.hoisted(() => ({
  mockGetContent: vi.fn<typeof import("./generated/imageShare")["getContent"]>(),
  mockGetContentPath: vi.fn<typeof import("./generated/imageShare")["getContentPath"]>(),
}));

vi.mock("./generated/imageShare", async (importOriginal) => {
  const actual = await importOriginal<typeof import("./generated/imageShare")>();
  return {
    ...actual,
    getContent: mockGetContent,
    getContentPath: mockGetContentPath,
  };
});

function pageResponse(
  items: FolderEntry[],
  page: number,
  pageSize: number,
  totalCount: number,
) {
  return {
    status: 200 as const,
    data: { items, page, pageSize, totalCount },
    headers: new Headers(),
  };
}

function createWrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

function renderContentHook(path: string | undefined) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return renderHook(() => useFolderContent(path), {
    wrapper: createWrapper(queryClient),
  });
}

describe("useFolderContent routing", () => {
  beforeEach(() => {
    mockGetContent.mockReset();
    mockGetContentPath.mockReset();
  });

  it(
    "calls getContent with lowercase page/pageSize for root listing",
    async () => {
      expect.assertions(2);
      // Arrange
      mockGetContent.mockResolvedValueOnce(pageResponse([], 1, 50, 0));

      // Act
      renderContentHook(undefined);
      await waitFor(() => {
        expect(mockGetContent).toHaveBeenCalledOnce();
      });

      // Assert
      expect(mockGetContent).toHaveBeenCalledWith(
        { page: 1, pageSize: 50 },
        expect.any(Object),
      );
    },
    2000,
  );

  it(
    "calls getContentPath with lowercase page/pageSize for subfolder",
    async () => {
      expect.assertions(2);
      // Arrange
      mockGetContentPath.mockResolvedValueOnce(pageResponse([], 1, 50, 0));

      // Act
      renderContentHook("photos/2024");
      await waitFor(() => {
        expect(mockGetContentPath).toHaveBeenCalledOnce();
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

describe("useFolderContent pagination", () => {
  beforeEach(() => {
    mockGetContent.mockReset();
    mockGetContentPath.mockReset();
  });

  it(
    "reports hasNextPage when more items remain",
    async () => {
      expect.hasAssertions();
      // Arrange — 100 total, page size 50, so page 1 of 2
      mockGetContent.mockResolvedValueOnce(pageResponse([{ name: "a", path: "a", type: "Folder" }], 1, 50, 100));

      // Act
      const { result } = renderContentHook(undefined);
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Assert
      expect(result.current.hasNextPage).toBe(true);
    },
    2000,
  );

  it(
    "reports no hasNextPage when on the last page",
    async () => {
      expect.hasAssertions();
      // Arrange — 30 total, page size 50, so page 1 of 1
      mockGetContent.mockResolvedValueOnce(pageResponse([], 1, 50, 30));

      // Act
      const { result } = renderContentHook(undefined);
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Assert
      expect(result.current.hasNextPage).toBe(false);
    },
    2000,
  );
});
