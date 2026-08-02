import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { type IUser } from "@lib/api/generated/imageShare.schemas";
import MetroAppBar from "@components/MetroAppBar";
import { type ReactNode } from "react";
import { ThemeProvider } from "@lib/themeContext";
import setupThemeEnvironment from "@test/themeEnvironment";

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

function authenticatedUser(overrides: Partial<IUser> = {}): IUser {
  return { isAuthenticated: true, isAdmin: false, name: "Jane", ...overrides };
}

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>{children}</ThemeProvider>
    </QueryClientProvider>
  );
}

interface RenderOptions {
  user?: IUser;
  breadcrumb?: ReactNode;
  children?: ReactNode;
}

function renderAppBar({ user = authenticatedUser(), breadcrumb, children }: RenderOptions = {}) {
  return render(
    <MetroAppBar user={user} breadcrumb={breadcrumb}>
      {children}
    </MetroAppBar>,
    { wrapper },
  );
}

describe("metroAppBar renders the app title and logo", () => {
  it("shows the ImageShare title next to the logo", () => {
    expect.assertions(2);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar();

    // Assert
    expect(screen.getByText("ImageShare")).toBeInTheDocument();
    expect(document.querySelector("svg")).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar renders the theme toggle", () => {
  it("includes a theme toggle button", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar();

    // Assert
    expect(screen.getByRole("button", { name: "Switch to dark theme" })).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar shows the user chip with the user name", () => {
  it("displays the authenticated user name", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ name: "Alice" }) });

    // Assert
    expect(screen.getByText("Alice")).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar falls back to User when name is undefined", () => {
  it("shows the fallback label when the user name is undefined", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ name: undefined }) });

    // Assert
    expect(screen.getByText("User")).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar hides the admin button for non-admin users", () => {
  it("does not render an admin button when isAdmin is false", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ isAdmin: false }) });

    // Assert
    expect(screen.queryByRole("button", { name: "Admin" })).toBeNull();
  }, 1000);
});

describe("metroAppBar shows the admin button for admin users", () => {
  it("renders an admin button when isAdmin is true", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ isAdmin: true }) });

    // Assert
    expect(screen.getByRole("button", { name: "Admin" })).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar hides the admin button when isAdmin is undefined", () => {
  it("does not render an admin button when isAdmin is not set", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ isAdmin: undefined }) });

    // Assert
    expect(screen.queryByRole("button", { name: "Admin" })).toBeNull();
  }, 1000);
});

describe("metroAppBar shows the share button for admin users", () => {
  it("renders a Share button when isAdmin is true", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ isAdmin: true }) });

    // Assert
    expect(screen.getByRole("button", { name: "Share" })).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar hides the share button for non-admin users", () => {
  it("does not render a Share button when isAdmin is false", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ user: authenticatedUser({ isAdmin: false }) });

    // Assert
    expect(screen.queryByRole("button", { name: "Share" })).toBeNull();
  }, 1000);
});

describe("metroAppBar renders the breadcrumb slot when provided", () => {
  it("displays breadcrumb content in the centre region", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ breadcrumb: <nav>Home / Browse</nav> });

    // Assert
    expect(screen.getByText("Home / Browse")).toBeInTheDocument();
  }, 1000);
});

describe("metroAppBar omits the breadcrumb region when not provided", () => {
  it("does not render a centre region when breadcrumb is undefined", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar();

    // Assert — only the theme toggle and user chip buttons exist, no nav
    expect(screen.queryByRole("navigation")).toBeNull();
  }, 1000);
});

describe("metroAppBar renders page content below the bar", () => {
  it("displays children in the main content area", () => {
    expect.assertions(1);
    // Arrange + Act
    setupThemeEnvironment(false);
    renderAppBar({ children: <p>Gallery content</p> });

    // Assert
    expect(screen.getByText("Gallery content")).toBeInTheDocument();
  }, 1000);
});
