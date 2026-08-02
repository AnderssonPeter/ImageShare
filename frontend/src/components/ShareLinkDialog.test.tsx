import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import Dialog from "@components/ui/Dialog";
import ShareLinkDialog from "@components/ShareLinkDialog";

type GenerateHandler = (params: { name: string; filter: string; endDate: string }) => void;

const FUTURE_DATE = "2099-12-31T23:59";

function renderOpenDialog(onGenerate: GenerateHandler): void {
  render(
    <Dialog.Dialog open>
      <ShareLinkDialog onGenerate={onGenerate} />
    </Dialog.Dialog>,
  );
}

describe("shareLinkDialog form fields", () => {
  it("renders Name, Filter, and End date inputs", () => {
    expect.assertions(3);
    // Arrange + Act
    renderOpenDialog(vi.fn<GenerateHandler>());
    // Assert
    expect(screen.getByLabelText("Name")).toBeInTheDocument();
    expect(screen.getByLabelText("Filter")).toBeInTheDocument();
    expect(screen.getByLabelText("End date")).toBeInTheDocument();
  }, 1000);
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
    // Act
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test" } });
    fireEvent.change(screen.getByLabelText("Filter"), { target: { value: "photos" } });
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

describe("shareLinkDialog submission", () => {
  it("calls onGenerate with trimmed values when the form is valid", async () => {
    expect.hasAssertions();
    // Arrange
    const onGenerate = vi.fn<GenerateHandler>();
    renderOpenDialog(onGenerate);
    // Act
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "  Test User  " } });
    fireEvent.change(screen.getByLabelText("Filter"), { target: { value: "  photos/2024  " } });
    fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
    fireEvent.click(screen.getByRole("button", { name: "Generate" }));
    // Assert
    await waitFor(() => {
      expect(onGenerate).toHaveBeenCalledWith({
        name: "Test User",
        filter: "photos/2024",
        endDate: FUTURE_DATE,
      });
    });
  }, 2000);
});
