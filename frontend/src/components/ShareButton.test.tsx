import { type FolderEntry, type generateToken, type getContent } from "@lib/api/generated";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ApiError } from "@lib/api/errors";
import { type ReactNode } from "react";
import ShareButton from "@components/ShareButton";

const { mockGenerateToken, mockGetContent } = vi.hoisted(() => ({
  mockGenerateToken: vi.fn<typeof generateToken>(),
  mockGetContent: vi.fn<typeof getContent>(),
}));

vi.mock(import("@lib/api/generated"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    generateToken: mockGenerateToken as never,
    getContent: mockGetContent as never,
  };
});

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.payload.signature";
const FUTURE_DATE = "2099-12-31T23:59";

function emptyPageResponse() {
  return {
    data: { items: [] as FolderEntry[], page: 1, pageSize: 500, totalCount: 0 },
    request: new Request("http://localhost/api/content"),
    response: new Response(),
  } as never;
}

function tokenResponse(token: string) {
  return {
    data: token,
    request: new Request("http://localhost/api/authentication/token/generate"),
    response: new Response(),
  } as never;
}

/** Mock: content listing succeeds, token endpoint returns the JWT. */
function stubTokenSuccess(): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockResolvedValue(tokenResponse(TOKEN));
}

/** Mock: content listing succeeds, token endpoint rejects with an ApiError. */
function stubTokenError(error: ApiError): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockRejectedValue(error);
}

/** Mock: content listing succeeds, token endpoint never resolves (stays pending). */
function stubTokenPending(): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockReturnValue(Promise.race<never>([]) as never);
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

function assertTokenQuery(): void {
  const [call] = mockGenerateToken.mock.calls;
  expect(call).toBeDefined();
  const query = call?.[0]?.query;
  expect(query).toMatchObject({ name: "Test User", filter: "*" });
  expect(query?.endDate).toBe(FUTURE_DATE);
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
  it("calls the token endpoint and opens the result dialog", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderShareButton();
    // Act — open the form and submit a valid form
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert — the result dialog opens (QR code appears) and the SDK was hit
    await waitFor(() => {
      expect(screen.getByText("Share link generated")).toBeInTheDocument();
    });
    assertTokenQuery();
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
    // The result dialog should not appear
    expect(screen.queryByText("Share link generated")).not.toBeInTheDocument();
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
    expect(screen.queryByText("Share link generated")).not.toBeInTheDocument();
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
