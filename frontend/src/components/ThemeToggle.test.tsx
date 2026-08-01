import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { type ReactNode } from "react";
import { ThemeProvider } from "@lib/themeContext";
import ThemeToggle from "@components/ThemeToggle";
import setupThemeEnvironment from "@test/themeEnvironment";

function wrapper({ children }: { children: ReactNode }) {
  return <ThemeProvider>{children}</ThemeProvider>;
}

function renderToggle() {
  return render(<ThemeToggle />, { wrapper });
}

/**
 * The trigger button's accessible name mirrors the toggle's target, so
 * querying by role reveals both the element and the next mode in one go.
 */
function triggerButton(): HTMLElement {
  return screen.getByRole("button");
}

describe("themeToggle reflects the effective theme with no override", () => {
  it('shows the sun and a "switch to dark" label when the system is light', () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderToggle();

    // Assert
    expect(triggerButton()).toHaveAttribute("aria-label", "Switch to dark theme");
  }, 1000);
});

describe("themeToggle switches from light to dark on click", () => {
  it("pins a dark override and updates the icon and label", () => {
    expect.assertions(3);
    // Arrange
    setupThemeEnvironment(false);
    renderToggle();

    // Act
    fireEvent.click(triggerButton());

    // Assert
    expect(triggerButton()).toHaveAttribute("aria-label", "Switch to light theme");
    expect(localStorage.getItem("theme")).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  }, 1000);
});

describe("themeToggle switches from dark to light on click", () => {
  it("pins a light override and removes the dark class", () => {
    expect.assertions(3);
    // Arrange
    setupThemeEnvironment(true);
    document.documentElement.classList.add("dark");
    renderToggle();

    // Act
    fireEvent.click(triggerButton());

    // Assert
    expect(triggerButton()).toHaveAttribute("aria-label", "Switch to dark theme");
    expect(localStorage.getItem("theme")).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  }, 1000);
});

describe("themeToggle reflects a stored override on mount", () => {
  it("shows the moon when an explicit dark override already exists", () => {
    expect.assertions(1);
    // Arrange
    setupThemeEnvironment(false);
    localStorage.setItem("theme", "dark");

    // Act
    renderToggle();

    // Assert
    expect(triggerButton()).toHaveAttribute("aria-label", "Switch to light theme");
  }, 1000);
});

describe("themeToggle requires a ThemeProvider", () => {
  it("throws when rendered without a provider", () => {
    expect.assertions(1);
    // Arrange
    setupThemeEnvironment(false);

    // Act + Assert
    expect(() => render(<ThemeToggle />)).toThrow(
      "useThemeContext must be used within a ThemeProvider",
    );
  }, 1000);
});
