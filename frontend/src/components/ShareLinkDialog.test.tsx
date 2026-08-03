import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import Dialog from "@components/ui/Dialog";
import { type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import { type ReactNode } from "react";
import ShareLinkDialog from "@components/ShareLinkDialog";
import { type getApiContent } from "@lib/api/generated/content/content";

type GenerateHandler = (params: { name: string; filter: string; endDate: string }) => void;

const FUTURE_DATE = "2099-12-31T23:59";

const ROOT_FOLDER_NAMES = ["documents", "photos", "videos"];

const { mockGetContent } = vi.hoisted(() => ({
  mockGetContent: vi.fn<typeof getApiContent>(),
}));

vi.mock(import("@lib/api/generated/content/content"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, getApiContent: mockGetContent };
});

function folderEntries(): FolderEntry[] {
  return ROOT_FOLDER_NAMES.map((name) => ({ name, path: name, type: "Folder" as const }));
}

function contentResponse() {
  return {
    status: 200 as const,
    data: { items: folderEntries(), page: 1, pageSize: 500, totalCount: ROOT_FOLDER_NAMES.length },
    headers: new Headers(),
  };
}

function renderOpenDialog(onGenerate: GenerateHandler): void {
  renderOpenDialogWithOptions(onGenerate, {});
}

function renderOpenDialogWithOptions(
  onGenerate: GenerateHandler,
  options: { submitError?: string; isSubmitting?: boolean },
): void {
  mockGetContent.mockReset();
  mockGetContent.mockResolvedValue(contentResponse());
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  render(
    <Dialog.Dialog open>
      <ShareLinkDialog onGenerate={onGenerate} submitError={options.submitError} isSubmitting={options.isSubmitting} />
    </Dialog.Dialog>,
    { wrapper: Wrapper },
  );
}

describe("shareLinkDialog form fields", () => {
  it("renders Name, the Filter builder, and End date inputs", async () => {
    expect.assertions(4);
    // Arrange + Act
    renderOpenDialog(vi.fn<GenerateHandler>());
    // Assert — Name and End date inputs plus the All folders checkbox render immediately
    expect(screen.getByLabelText("Name")).toBeInTheDocument();
    expect(screen.getByLabelText("End date")).toBeInTheDocument();
    expect(screen.getByLabelText("All folders")).toBeInTheDocument();
    // The root-folder rows appear once the content query resolves
    await waitFor(() => {
      expect(screen.getByLabelText("photos")).toBeInTheDocument();
    });
  }, 2000);
});

describe("shareLinkDialog validation", () => {
  it("shows errors for all empty fields on submit", async () => {
    expect.hasAssertions();
    // Arrange
    renderOpenDialog(vi.fn<GenerateHandler>());
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    // Assert
    await expect(screen.findByText("A name must be specified.")).resolves.toBeInTheDocument();
    expect(screen.getByText("A filter must be specified.")).toBeInTheDocument();
    expect(screen.getByText("An end date must be specified.")).toBeInTheDocument();
  }, 2000);

  it("does not call onGenerate when the form is invalid", async () => {
    expect.hasAssertions();
    // Arrange
    const onGenerate = vi.fn<GenerateHandler>();
    renderOpenDialog(onGenerate);
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    await waitFor(() => {
      expect(screen.getByText("A name must be specified.")).toBeInTheDocument();
    });
    // Assert
    expect(onGenerate).not.toHaveBeenCalled();
  }, 2000);

  it("rejects an end date in the past", async () => {
    expect.hasAssertions();
    // Arrange
    renderOpenDialog(vi.fn<GenerateHandler>());
    // Act — wait for the root folders, then fill a valid name + filter + past date
    await waitFor(() => {
      expect(screen.getByLabelText("photos")).toBeInTheDocument();
    });
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test" } });
    fireEvent.click(screen.getByLabelText("photos"));
    fireEvent.change(screen.getByLabelText("End date"), { target: { value: "2020-01-01T00:00" } });
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    // Assert
    await expect(screen.findByText("The end date must be in the future.")).resolves.toBeInTheDocument();
  }, 2000);
});

describe("shareLinkDialog error clearing", () => {
  it("clears an error when the field is corrected after a failed submit", async () => {
    expect.hasAssertions();
    // Arrange
    renderOpenDialog(vi.fn<GenerateHandler>());
    // Act — submit empty, then fix the Name field
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    await expect(screen.findByText("A name must be specified.")).resolves.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test User" } });
    // Assert
    await waitFor(() => {
      expect(screen.queryByText("A name must be specified.")).not.toBeInTheDocument();
    });
  }, 2000);
});

describe("shareLinkDialog submit error", () => {
  it("displays the submit error message when provided", () => {
    expect.assertions(1);
    // Arrange + Act
    renderOpenDialogWithOptions(vi.fn<GenerateHandler>(), { submitError: "A filter must be specified." });
    // Assert
    expect(screen.getByText("A filter must be specified.")).toBeInTheDocument();
  }, 1000);

  it("disables the Generate button and shows loading text while submitting", () => {
    expect.assertions(2);
    // Arrange + Act
    renderOpenDialogWithOptions(vi.fn<GenerateHandler>(), { isSubmitting: true });
    // Assert
    const button = screen.getByRole("button", { name: "Generating…" });
    expect(button).toBeDisabled();
    expect(button).toBeInTheDocument();
  }, 1000);
});

describe("shareLinkDialog submission", () => {
  it("calls onGenerate with the selected filter when the form is valid", async () => {
    expect.hasAssertions();
    // Arrange
    const onGenerate = vi.fn<GenerateHandler>();
    renderOpenDialog(onGenerate);
    await waitFor(() => {
      expect(screen.getByLabelText("photos")).toBeInTheDocument();
    });
    // Act — name + a single allowed folder + future date
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "  Test User  " } });
    fireEvent.click(screen.getByLabelText("photos"));
    fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    // Assert
    await waitFor(() => {
      expect(onGenerate).toHaveBeenCalledWith({
        name: "Test User",
        filter: "photos",
        endDate: FUTURE_DATE,
      });
    });
  }, 2000);

  it("encodes All folders with denied folders as '*|!folder'", async () => {
    expect.hasAssertions();
    // Arrange
    const onGenerate = vi.fn<GenerateHandler>();
    renderOpenDialog(onGenerate);
    await waitFor(() => {
      expect(screen.getByLabelText("photos")).toBeInTheDocument();
    });
    // Act — All folders selected, then photos denied
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test" } });
    fireEvent.click(screen.getByLabelText("All folders"));
    fireEvent.click(screen.getByLabelText("photos"));
    fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    // Assert
    await waitFor(() => {
      expect(onGenerate).toHaveBeenCalledWith({ name: "Test", filter: "*|!photos", endDate: FUTURE_DATE });
    });
  }, 2000);
});
