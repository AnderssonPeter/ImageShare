import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import Breadcrumb from "@components/Breadcrumb";
import { type ReactNode } from "react";

function MockLink({
  to,
  params,
  className,
  children,
}: {
  to: string;
  params: { _splat: string | undefined };
  className?: string;
  children: ReactNode;
}) {
  return (
    <a href={to} data-splat={params._splat} className={className}>
      {children}
    </a>
  );
}

vi.mock(import("@tanstack/react-router"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, Link: MockLink as never };
});

describe("breadcrumb at root", () => {
  it("shows only the current Home crumb with no links", () => {
    expect.assertions(2);
    // Arrange + Act
    render(<Breadcrumb />);

    // Assert
    expect(screen.getByText("Home")).toHaveAttribute("aria-current", "page");
    expect(screen.queryAllByRole("link")).toHaveLength(0);
  }, 1000);
});

describe("breadcrumb for a single segment", () => {
  it("links Home to the root and marks the segment as current", () => {
    expect.assertions(3);
    // Arrange + Act
    render(<Breadcrumb path="Birds" />);

    // Assert
    expect(screen.getByText("Birds")).toHaveAttribute("aria-current", "page");
    const links = screen.getAllByRole("link");
    expect(links).toHaveLength(1);
    expect(links[0]).not.toHaveAttribute("data-splat");
  }, 1000);
});

describe("breadcrumb for nested segments", () => {
  it("links each ancestor to its cumulative path and marks the last as current", () => {
    expect.assertions(5);
    // Arrange + Act
    render(<Breadcrumb path="photos/2024/trip" />);

    // Assert
    expect(screen.getByText("trip")).toHaveAttribute("aria-current", "page");
    const links = screen.getAllByRole("link");
    expect(links).toHaveLength(3);
    expect(links[0]).not.toHaveAttribute("data-splat");
    expect(links[1]).toHaveAttribute("data-splat", "photos");
    expect(links[2]).toHaveAttribute("data-splat", "photos/2024");
  }, 1000);
});
