import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { type ReactNode } from "react";
import ShareButton from "@components/ShareButton";
import { type customFetcher } from "@lib/api/customFetcher";

const { mockFetcher } = vi.hoisted(() => ({
  mockFetcher: vi.fn<typeof customFetcher>(),
}));

vi.mock(import("@lib/api/customFetcher"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, customFetcher: mockFetcher as unknown as typeof customFetcher };
});

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.payload.signature";
const FUTURE_DATE = "2099-12-31T23:59";

function renderShareButton(onToken?: (token: string) => void): void {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  render(<ShareButton onToken={onToken} />, { wrapper: Wrapper });
}

function fillAndSubmitForm(): void {
  fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test User" } });
  fireEvent.change(screen.getByLabelText("Filter"), { target: { value: "photos/2024" } });
  fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
  fireEvent.click(screen.getByRole("button", { name: "Generate" }));
}

function assertTokenUrl(): void {
  const [[url]] = mockFetcher.mock.calls;
  expect(url).toContain("name=Test+User");
  expect(url).toContain("filter=photos%2F2024");
  expect(url).toContain(`endDate=${encodeURIComponent(FUTURE_DATE)}`);
}

describe("shareButton trigger", () => {
  it("renders the Share trigger button", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    renderShareButton();
    expect(screen.getByRole("button", { name: "Share" })).toBeInTheDocument();
  }, 1000);
});

describe("shareButton token generation", () => {
  it("calls the token endpoint and shows the JWT in the result dialog", async () => {
    expect.hasAssertions();
    // Arrange
    mockFetcher.mockReset();
    mockFetcher.mockResolvedValue({ status: 200, data: TOKEN, headers: new Headers() });
    renderShareButton();
    // Act — open the form and submit a valid form
    fireEvent.click(screen.getByRole("button", { name: "Share" }));
    fillAndSubmitForm();
    // Assert — the fetcher is hit with the encoded query params
    await waitFor(() => {
      expect(mockFetcher).toHaveBeenCalledTimes(1);
    });
    assertTokenUrl();
    await waitFor(() => {
      expect(screen.getByText(TOKEN)).toBeInTheDocument();
    });
  }, 2000);

  it("fires onToken with the JWT once generation succeeds", async () => {
    expect.hasAssertions();
    // Arrange
    mockFetcher.mockReset();
    mockFetcher.mockResolvedValue({ status: 200, data: TOKEN, headers: new Headers() });
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
