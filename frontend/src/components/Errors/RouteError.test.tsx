import { ApiError, type ProblemDetails } from "@lib/api/errors";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { type ReactNode } from "react";
import RouteError from "@components/Errors/RouteError";

const { mockInvalidate } = vi.hoisted(() => {
  const invalidate = vi.fn<() => Promise<void>>();
  return { mockInvalidate: invalidate };
});

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
  return {
    ...actual,
    Link: MockLink as never,
    useRouter: (() => ({ invalidate: mockInvalidate })) as never,
  };
});

describe("route error redirect", () => {
  it("redirects to the backend login endpoint on a 401", () => {
    expect.assertions(2);
    // Arrange
    const replaceMock = vi.fn<(url: string) => void>();
    vi.stubGlobal("location", {
      pathname: "/",
      search: "",
      hash: "",
      replace: replaceMock,
    });
    const error = new ApiError(401, undefined);

    // Act
    render(<RouteError error={error} reset={vi.fn<() => void>()} />);

    // Assert
    expect(replaceMock).toHaveBeenCalledWith(
      `/api/authentication/login?returnUrl=${encodeURIComponent("/")}`,
    );
    expect(screen.getByText("Redirecting to sign in…")).toBeInTheDocument();
    vi.unstubAllGlobals();
  }, 1000);
});

describe("route error message", () => {
  it("shows the RFC 7807 detail for a 403", () => {
    expect.assertions(1);
    // Arrange
    const problem: ProblemDetails = { title: "Forbidden", detail: "Not allowed." };
    const error = new ApiError(403, problem);

    // Act
    render(<RouteError error={error} reset={vi.fn<() => void>()} />);

    // Assert
    expect(screen.getByText("Not allowed.")).toBeInTheDocument();
  }, 1000);

  it("shows a not-found message for a 404 thrown from a loader", () => {
    expect.assertions(1);
    // Arrange
    const error = new ApiError(404, undefined);

    // Act
    render(<RouteError error={error} reset={vi.fn<() => void>()} />);

    // Assert
    expect(screen.getByText("We couldn't find what you were looking for.")).toBeInTheDocument();
  }, 1000);

  it("falls back to the error message for a non-ApiError", () => {
    expect.assertions(1);
    // Arrange
    const error = new TypeError("network gone");

    // Act
    render(<RouteError error={error} reset={vi.fn<() => void>()} />);

    // Assert
    expect(screen.getByText("network gone")).toBeInTheDocument();
  }, 1000);
});

describe("route error actions", () => {
  it("resets the boundary and invalidates the router when Retry is pressed", () => {
    expect.assertions(2);
    // Arrange
    const mockReset = vi.fn<() => void>();
    mockInvalidate.mockReset();
    render(<RouteError error={new ApiError(500, { detail: "boom" })} reset={mockReset} />);

    // Act
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));

    // Assert
    expect(mockReset).toHaveBeenCalledTimes(1);
    expect(mockInvalidate).toHaveBeenCalledTimes(1);
  }, 1000);

  it("links back to the browse root", () => {
    expect.assertions(2);
    // Arrange + Act
    render(
      <RouteError error={new ApiError(500, { detail: "boom" })} reset={vi.fn<() => void>()} />,
    );

    // Assert
    const link = screen.getByRole("link", { name: "Go to library" });
    expect(link).toHaveAttribute("href", "/browse/$");
    expect(link).not.toHaveAttribute("data-splat");
  }, 1000);
});
