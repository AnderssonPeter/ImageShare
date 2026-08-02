import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { useCallback, useState } from "react";
import FilterBuilder from "@components/FilterBuilder";

interface FilterCapture {
  filter: string;
}

function renderBuilder(folders: string[], capture: FilterCapture): void {
  function Wrapper() {
    const [value, setValue] = useState("");
    const handleChange = useCallback((next: string) => {
      capture.filter = next;
      setValue(next);
    }, []);
    return <FilterBuilder folders={folders} value={value} onChange={handleChange} />;
  }
  render(<Wrapper />);
}

describe("filterBuilder allow selection", () => {
  it("encodes a single selected folder as its name", () => {
    expect.assertions(1);
    // Arrange
    const capture: FilterCapture = { filter: "" };
    renderBuilder(["photos", "videos"], capture);
    // Act
    fireEvent.click(screen.getByLabelText("photos"));
    // Assert
    expect(capture.filter).toBe("photos");
  }, 1000);

  it("encodes multiple selected folders joined by '|'", () => {
    expect.assertions(1);
    // Arrange
    const capture: FilterCapture = { filter: "" };
    renderBuilder(["photos", "videos"], capture);
    // Act
    fireEvent.click(screen.getByLabelText("photos"));
    fireEvent.click(screen.getByLabelText("videos"));
    // Assert
    expect(capture.filter).toBe("photos|videos");
  }, 1000);
});

describe("filterBuilder all folders", () => {
  it("encodes All folders as '*'", () => {
    expect.assertions(1);
    // Arrange
    const capture: FilterCapture = { filter: "" };
    renderBuilder(["photos"], capture);
    // Act
    fireEvent.click(screen.getByLabelText("All folders"));
    // Assert
    expect(capture.filter).toBe("*");
  }, 1000);

  it("encodes a denied folder as '!name' after All folders", () => {
    expect.assertions(1);
    // Arrange
    const capture: FilterCapture = { filter: "" };
    renderBuilder(["photos", "videos"], capture);
    // Act — select All folders, then deny photos
    fireEvent.click(screen.getByLabelText("All folders"));
    fireEvent.click(screen.getByLabelText("photos"));
    // Assert
    expect(capture.filter).toBe("*|!photos");
  }, 1000);
});
