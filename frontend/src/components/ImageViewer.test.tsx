import {
  type FolderEntry,
  type PaginatedResultOfFolderEntry,
  type getContentByPath,
} from "@lib/api/generated";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import ImageViewer from "@components/ImageViewer";
import { type ReactNode } from "react";
import { type imageUrl } from "@lib/api/urls";

const { mockImageUrl, mockGetContentByPath } = vi.hoisted(() => ({
  mockImageUrl: vi.fn<typeof imageUrl>(),
  mockGetContentByPath: vi.fn<typeof getContentByPath>(),
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, imageUrl: mockImageUrl };
});

vi.mock(import("@lib/api/generated/sdk.gen"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, getContentByPath: mockGetContentByPath as never };
});

function imageEntry(name: string, path: string): FolderEntry {
  return { name, path, type: "File" };
}

function pageData(items: FolderEntry[]): PaginatedResultOfFolderEntry {
  return { items, page: 1, pageSize: 50, totalCount: items.length };
}

function sdkResponse(items: FolderEntry[]) {
  return {
    data: pageData(items),
    request: new Request("http://localhost/api/content/photos"),
    response: new Response(),
  } as never;
}

function setupContent(images: FolderEntry[]): void {
  mockImageUrl.mockReset();
  mockGetContentByPath.mockReset();
  mockImageUrl.mockReturnValue("/img");
  mockGetContentByPath.mockResolvedValue(sdkResponse(images));
}

function noop(): void {}

function renderViewer(props: {
  folderPath?: string;
  imagePath: string;
  onClose?: () => void;
}): void {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  render(
    <Wrapper>
      <ImageViewer
        folderPath={props.folderPath}
        imagePath={props.imagePath}
        onClose={props.onClose ?? noop}
      />
    </Wrapper>,
  );
}

describe("imageViewer close button", () => {
  it("fires onClose when the close button is clicked", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "a.jpg")]);
    const onClose = vi.fn<() => void>();

    // Act
    renderViewer({ folderPath: "photos", imagePath: "a.jpg", onClose });
    fireEvent.click(await screen.findByRole("button", { name: "Close" }));

    // Assert
    expect(onClose).toHaveBeenCalledTimes(1);
  }, 2000);
});

describe("imageViewer slides", () => {
  it("renders a slide for each image in the folder", async () => {
    expect.assertions(2);
    // Arrange
    setupContent([imageEntry("a", "photos/a.jpg"), imageEntry("b", "photos/b.jpg")]);

    // Act
    renderViewer({ folderPath: "photos", imagePath: "photos/a.jpg" });
    await screen.findByAltText("a");

    // Assert
    expect(screen.getByAltText("a")).toBeInTheDocument();
    expect(screen.getByAltText("b")).toBeInTheDocument();
  }, 2000);

  it("uses full-resolution image URLs for slides", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "photos/a.jpg")]);

    // Act
    renderViewer({ folderPath: "photos", imagePath: "photos/a.jpg" });
    await screen.findByAltText("a");

    // Assert
    expect(mockImageUrl).toHaveBeenCalledWith("photos/a.jpg", false);
  }, 2000);

  it("opens without error when the clicked image is not the first slide", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "p/a.jpg"), imageEntry("b", "p/b.jpg")]);

    // Act
    renderViewer({ folderPath: "p", imagePath: "p/b.jpg" });
    await screen.findByAltText("b");

    // Assert
    expect(screen.getByAltText("a")).toBeInTheDocument();
  }, 2000);
});

describe("imageViewer keyboard navigation", () => {
  it("fires onClose when Escape is pressed", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "a.jpg")]);
    const onClose = vi.fn<() => void>();

    // Act
    renderViewer({ folderPath: "photos", imagePath: "a.jpg", onClose });
    await screen.findByAltText("a");
    fireEvent.keyDown(document, { key: "Escape" });

    // Assert
    expect(onClose).toHaveBeenCalledTimes(1);
  }, 2000);

  it("does not fire onClose for other keys", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "a.jpg")]);
    const onClose = vi.fn<() => void>();

    // Act
    renderViewer({ folderPath: "photos", imagePath: "a.jpg", onClose });
    await screen.findByAltText("a");
    fireEvent.keyDown(document, { key: "ArrowRight" });

    // Assert
    expect(onClose).not.toHaveBeenCalled();
  }, 2000);
});
