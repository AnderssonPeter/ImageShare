import { type FolderEntry, type getContentByPath } from "@lib/api/generated";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import ImageViewer from "@components/ImageViewer";
import { type ReactNode } from "react";
import { type imageUrl } from "@lib/api/urls";
import { type useCanLoadImages } from "@lib/api/usageAgreement";

const { mockImageUrl, mockGetContentByPath, mockCanLoadImages } = vi.hoisted(() => ({
  mockImageUrl: vi.fn<typeof imageUrl>(),
  mockGetContentByPath: vi.fn<typeof getContentByPath>(),
  mockCanLoadImages: vi.fn<typeof useCanLoadImages>(),
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, imageUrl: mockImageUrl };
});

vi.mock(import("@lib/api/generated/sdk.gen"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, getContentByPath: mockGetContentByPath as never };
});

vi.mock(import("@lib/api/usageAgreement"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, useCanLoadImages: mockCanLoadImages };
});

function imageEntry(name: string, path: string): FolderEntry {
  return { name, path, type: "File" };
}

function sdkResponse(items: FolderEntry[]) {
  return {
    data: items,
    request: new Request("http://localhost/api/content/photos"),
    response: new Response(),
  } as never;
}

function setupContent(images: FolderEntry[]): void {
  mockImageUrl.mockReset();
  mockGetContentByPath.mockReset();
  mockCanLoadImages.mockReset();
  mockImageUrl.mockReturnValue("/img");
  mockGetContentByPath.mockResolvedValue(sdkResponse(images));
  mockCanLoadImages.mockReturnValue(true);
}

function noop(): void {}

function renderViewer(props: {
  folderPath?: string;
  imageName: string;
  onClose?: () => void;
  onShare?: () => void;
}): void {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  render(
    <Wrapper>
      <ImageViewer
        folderPath={props.folderPath}
        imageName={props.imageName}
        onClose={props.onClose ?? noop}
        onShare={props.onShare}
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
    renderViewer({ folderPath: "photos", imageName: "a", onClose });
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
    renderViewer({ folderPath: "photos", imageName: "a" });
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
    renderViewer({ folderPath: "photos", imageName: "a" });
    await screen.findByAltText("a");

    // Assert
    expect(mockImageUrl).toHaveBeenCalledWith("photos/a.jpg", false);
  }, 2000);

  it("opens without error when the clicked image is not the first slide", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "p/a.jpg"), imageEntry("b", "p/b.jpg")]);

    // Act
    renderViewer({ folderPath: "p", imageName: "b" });
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
    renderViewer({ folderPath: "photos", imageName: "a", onClose });
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
    renderViewer({ folderPath: "photos", imageName: "a", onClose });
    await screen.findByAltText("a");
    fireEvent.keyDown(document, { key: "ArrowRight" });

    // Assert
    expect(onClose).not.toHaveBeenCalled();
  }, 2000);
});

describe("imageViewer share button", () => {
  it("fires onShare when the share button is clicked", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "a.jpg")]);
    const onShare = vi.fn<() => void>();

    // Act
    renderViewer({ folderPath: "photos", imageName: "a", onShare });
    fireEvent.click(await screen.findByRole("button", { name: "Share image" }));

    // Assert
    expect(onShare).toHaveBeenCalledTimes(1);
  }, 2000);

  it("does not render a share button when onShare is not provided", async () => {
    expect.assertions(1);
    // Arrange
    setupContent([imageEntry("a", "a.jpg")]);

    // Act
    renderViewer({ folderPath: "photos", imageName: "a" });
    await screen.findByAltText("a");

    // Assert
    expect(screen.queryByRole("button", { name: "Share image" })).toBeNull();
  }, 2000);
});

describe("imageViewer usage-agreement gate", () => {
  it("does not request full-res image URLs until the agreement allows loading", async () => {
    expect.assertions(1);
    // Arrange — agreement pending: full-res serving is 403-gated on the backend.
    setupContent([imageEntry("a", "photos/a.jpg")]);
    mockCanLoadImages.mockReturnValue(false);

    // Act
    renderViewer({ folderPath: "photos", imageName: "a" });
    await screen.findByAltText("a");

    // Assert — no full-res URL built while the agreement is unaccepted
    expect(mockImageUrl).not.toHaveBeenCalledWith("photos/a.jpg", false);
  }, 2000);
});
