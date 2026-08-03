import { ApiError, type customFetcher } from "@lib/api/customFetcher";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { type ReactNode } from "react";
import ShareButton from "@components/ShareButton";

const { mockFetcher } = vi.hoisted(() => ({
  mockFetcher: vi.fn<typeof customFetcher>(),
}));

vi.mock(import("@lib/api/customFetcher"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, customFetcher: mockFetcher as unknown as typeof customFetcher };
});

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.payload.signature";
const FUTURE_DATE = "2099-12-31T23:59";

const CONTENT_RESPONSE = {
  status: 200,
  data: { items: [], page: 1, pageSize: 500, totalCount: 0 },
  headers: new Headers(),
} as never;

const TOKEN_RESPONSE = { status: 200, data: TOKEN, headers: new Headers() } as never;

/** Resolve content calls to an empty page, token calls to the JWT. */
function routeTokenSuccess(url: string): never {
  return (url.startsWith("/api/content") ? CONTENT_RESPONSE : TOKEN_RESPONSE) as never;
}

/** Resolve content calls to an empty page, reject token calls with an ApiError. */
function routeTokenError(url: string, error: ApiError): never {
  if (url.startsWith("/api/content")) {
    return CONTENT_RESPONSE;
  }
  throw error;
}

/** Resolve content calls, leave token calls pending indefinitely. */
function routeTokenPending(url: string): never {
  if (url.startsWith("/api/content")) {
    return CONTENT_RESPONSE;
  }
  return Promise.race<never>([]) as never;
}

/** Mock: content listing succeeds, token endpoint returns the JWT. */
function stubTokenSuccess(): void {
  mockFetcher.mockImplementation(routeTokenSuccess);
}

/** Mock: content listing succeeds, token endpoint rejects with an ApiError. */
function stubTokenError(error: ApiError): void {
  mockFetcher.mockImplementation((url) => routeTokenError(url, error));
}

/** Mock: content listing succeeds, token endpoint never resolves (stays pending). */
function stubTokenPending(): void {
  mockFetcher.mockImplementation(routeTokenPending);
}

function renderShareButton(onToken?: (token: string) => void): void {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  render(<ShareButton onToken={onToken} />, { wrapper: Wrapper });
}

function fillAndSubmitForm(): void {
  fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test User" } });
  fireEvent.click(screen.getByLabelText("All folders"));
  fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
  fireEvent.click(screen.getByRole("button", { name: "Generate" }));
}

function assertTokenUrl(): void {
  const tokenCall = mockFetcher.mock.calls.find(([url]) => url.includes("/token/generate"));
  expect(tokenCall).toBeDefined();
  const url = tokenCall?.[0] ?? "";
  expect(url).toContain("name=Test+User");
  expect(url).toContain("filter=*&");
  expect(url).toContain(`endDate=${encodeURIComponent(FUTURE_DATE)}`);
}

describe("shareButton trigger", () => {
  it("renders the Share trigger button", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    stubTokenSuccess();
    renderShareButton();
    expect(screen.getByRole("button", { name: "Share" })).toBeInTheDocument();
  }, 1000);
});

describe("shareButton token generation", () => {
  it("calls the token endpoint and shows the JWT in the result dialog", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderShareButton();
    // Act — open the form and submit a valid form
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert — the JWT appears and the fetcher was hit with the encoded query params
    await waitFor(() => {
      expect(screen.getByText(TOKEN)).toBeInTheDocument();
    });
    assertTokenUrl();
  }, 2000);

  it("fires onToken with the JWT once generation succeeds", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    const onToken = vi.fn<(token: string) => void>();
    renderShareButton(onToken);
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert
    await waitFor(() => {
      expect(onToken).toHaveBeenCalledWith(TOKEN);
    });
  }, 2000);
});

describe("shareButton error handling", () => {
  it("surfaces a 400 RFC 7807 error message in the form dialog", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenError(new ApiError(400, { detail: "A filter must be specified." }));
    renderShareButton();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert — the error message appears inside the still-open form dialog
    await waitFor(() => {
      expect(screen.getByText("A filter must be specified.")).toBeInTheDocument();
    });
    // The result dialog (JWT) should not appear
    expect(screen.queryByText(TOKEN)).not.toBeInTheDocument();
  }, 2000);

  it("surfaces a 403 RFC 7807 error message in the form dialog", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenError(new ApiError(403, { detail: "Forbidden." }));
    renderShareButton();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert
    await waitFor(() => {
      expect(screen.getByText("Forbidden.")).toBeInTheDocument();
    });
    expect(screen.queryByText(TOKEN)).not.toBeInTheDocument();
  }, 2000);

  it("shows a loading state on the Generate button while submitting", async () => {
    expect.hasAssertions();
    // Arrange — never-resolving token call keeps the mutation pending
    stubTokenPending();
    renderShareButton();
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    // Act
    fillAndSubmitForm();
    // Assert — the button switches to its loading label
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Generating…" })).toBeDisabled();
    });
  }, 2000);
});
