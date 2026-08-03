import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import ContentGrid from "@components/ContentGrid";
import { type FolderEntry } from "@lib/api/generated";
import { type useFolderContent } from "@lib/api/contentQueries";

const { mockUseFolderContent } = vi.hoisted(() => ({
  mockUseFolderContent: vi.fn<typeof useFolderContent>(),
}));

vi.mock(import("@lib/api/contentQueries"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, useFolderContent: mockUseFolderContent };
});

function noop(): void {}

function folderEntry(name: string, path: string): FolderEntry {
  return { name, path, type: "Folder" };
}

function setContent(result: Partial<ReturnType<typeof useFolderContent>>): void {
  mockUseFolderContent.mockReturnValue({
    data: undefined,
    fetchNextPage: noop,
    hasNextPage: false,
    isFetchingNextPage: false,
    isPending: false,
    ...result,
  } as ReturnType<typeof useFolderContent>);
}

describe("contentGrid skeleton while loading", () => {
  it("renders skeleton tiles and locks the scrollbar while the first page is pending", () => {
    expect.assertions(3);
    // Arrange
    setContent({ isPending: true, data: undefined });

    // Act
    const { container } = render(
      <ContentGrid onNavigateFolder={noop} onImageOpen={noop} />,
    );

    // Assert
    const skeletons = container.querySelectorAll('[data-slot="skeleton"]');
    expect(skeletons.length).toBeGreaterThan(0);
    const scrollContainer = container.firstElementChild;
    expect(scrollContainer).not.toBeNull();
    expect(scrollContainer?.className).toContain("overflow-hidden");
  }, 1000);
});

describe("contentGrid skeleton covers the full viewport", () => {
  it("scales skeleton tile count to the measured container height and width", () => {
    expect.assertions(1);
    // Arrange — mock element dimensions so the grid shape is deterministic:
    // 760×800 → columns 4, rows 7 → 28 skeleton tiles fill the screen.
    const heightSpy = vi
      .spyOn(HTMLElement.prototype, "clientHeight", "get")
      .mockReturnValue(800);
    const widthSpy = vi
      .spyOn(HTMLElement.prototype, "clientWidth", "get")
      .mockReturnValue(760);
    setContent({ isPending: true, data: undefined });

    // Act
    const { container } = render(
      <ContentGrid onNavigateFolder={noop} onImageOpen={noop} />,
    );

    // Assert — columns (4) × skeletonRows (7) = 28 tiles fill the screen
    const skeletons = container.querySelectorAll('[data-slot="skeleton"]');
    expect(skeletons).toHaveLength(28);

    // Cleanup
    heightSpy.mockRestore();
    widthSpy.mockRestore();
  }, 1000);
});

describe("contentGrid restores scrolling once loaded", () => {
  it("switches the scroll container back to overflow-auto when data is available", () => {
    expect.assertions(1);
    // Arrange
    const items = [folderEntry("Holidays", "holidays"), folderEntry("Cats", "cats")];
    setContent({
      isPending: false,
      data: {
        pages: [{ items, page: 1, pageSize: 50, totalCount: items.length }],
        pageParams: [1],
      },
    });

    // Act
    const { container } = render(
      <ContentGrid onNavigateFolder={noop} onImageOpen={noop} />,
    );

    // Assert — scrollbar is restored (the skeleton lock is released)
    const scrollContainer = container.firstElementChild;
    expect(scrollContainer?.className).toContain("overflow-auto");
  }, 1000);
});

describe("contentGrid empty state", () => {
  it("shows an empty-folder message when the loaded folder has no items", () => {
    expect.assertions(1);
    // Arrange
    setContent({
      isPending: false,
      data: { pages: [{ items: [], page: 1, pageSize: 50, totalCount: 0 }], pageParams: [1] },
    });

    // Act
    render(<ContentGrid onNavigateFolder={noop} onImageOpen={noop} />);

    // Assert
    expect(screen.getByText("This folder is empty.")).toBeInTheDocument();
  }, 1000);
});
