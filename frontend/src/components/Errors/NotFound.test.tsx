import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import NotFound from "@components/Errors/NotFound";
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
}): React.JSX.Element {
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

describe("not found page", () => {
  it("shows a not-found message", () => {
    expect.assertions(1);
    // Arrange + Act
    render(<NotFound />);

    // Assert
    expect(screen.getByText("This page could not be found.")).toBeInTheDocument();
  }, 1000);

  it("links back to the browse root", () => {
    expect.assertions(2);
    // Arrange + Act
    render(<NotFound />);

    // Assert
    const link = screen.getByRole("link", { name: "Go to library" });
    expect(link).toHaveAttribute("href", "/browse/$");
    expect(link).not.toHaveAttribute("data-splat");
  }, 1000);
});
