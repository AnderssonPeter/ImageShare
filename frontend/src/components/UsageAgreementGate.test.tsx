import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  type UsageAgreementResponse,
  type acceptUsageAgreement,
  type getUsageAgreement,
} from "@lib/api/generated";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ApiError } from "@lib/api/errors";
import { type ReactNode } from "react";
import UsageAgreementGate from "@components/UsageAgreementGate";

const { mockGetUsageAgreement, mockAcceptUsageAgreement } = vi.hoisted(() => ({
  mockGetUsageAgreement: vi.fn<typeof getUsageAgreement>(),
  mockAcceptUsageAgreement: vi.fn<typeof acceptUsageAgreement>(),
}));

vi.mock(import("@lib/api/generated/sdk.gen"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    getUsageAgreement: mockGetUsageAgreement as never,
    acceptUsageAgreement: mockAcceptUsageAgreement as never,
  };
});

const AGREEMENT: UsageAgreementResponse = {
  language: "en",
  text: "Line one of the agreement.\nLine two of the agreement.",
  accepted: false,
};

function sdkResponse<TData = undefined>(data?: TData) {
  return {
    data,
    request: new Request("http://localhost/api/usage-agreement"),
    response: new Response(),
  } as never;
}

function renderGate() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  return render(<UsageAgreementGate />, { wrapper: Wrapper });
}

describe("usage agreement gate disabled feature", () => {
  it("renders no dialog when the endpoint returns 404", async () => {
    expect.assertions(1);
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockRejectedValueOnce(new ApiError(404, undefined));

    // Act
    renderGate();

    // Assert
    await waitFor(() => {
      expect(screen.queryByText(AGREEMENT.text)).not.toBeInTheDocument();
    });
  }, 2000);
});

describe("usage agreement gate loading", () => {
  it("shows a loading overlay while the agreement probe is in flight", () => {
    expect.assertions(1);
    // Arrange — a never-resolving promise keeps the query in pending state.
    // `Promise.withResolvers` is used instead of `new Promise` (which the
    // `promise/avoid-new` rule disallows); see GridBackground.test.tsx.
    const { promise } = (
      Promise as unknown as { withResolvers: <TValue>() => { promise: Promise<TValue> } }
    ).withResolvers<never>();
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockReturnValueOnce(promise as never);

    // Act
    renderGate();

    // Assert
    expect(screen.getByTestId("usage-agreement-loading")).toBeInTheDocument();
  }, 2000);
});

describe("usage agreement gate already accepted", () => {
  it("renders no dialog when the agreement is already accepted", async () => {
    expect.assertions(1);
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: true }));

    // Act
    renderGate();

    // Assert
    await waitFor(() => {
      expect(screen.queryByText(AGREEMENT.text)).not.toBeInTheDocument();
    });
  }, 2000);
});

describe("usage agreement gate not accepted", () => {
  it("shows the agreement dialog", async () => {
    expect.assertions(1);
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: false }));

    // Act
    renderGate();

    // Assert
    await expect(screen.findByTestId("usage-agreement-text")).resolves.toHaveTextContent(
      /Line one of the agreement\.\s+Line two of the agreement\./u,
    );
  }, 2000);

  it("accepts the agreement and unmounts the dialog", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockAcceptUsageAgreement.mockReset();
    mockAcceptUsageAgreement.mockResolvedValueOnce(sdkResponse());
    mockGetUsageAgreement
      .mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: false }))
      .mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: true }));

    // Act
    renderGate();
    const acceptButton = await screen.findByRole("button", { name: "Accept" });
    fireEvent.click(acceptButton);

    // Assert
    await waitFor(() => {
      expect(mockAcceptUsageAgreement).toHaveBeenCalledTimes(1);
    });
    await waitFor(() => {
      expect(screen.queryByTestId("usage-agreement-text")).not.toBeInTheDocument();
    });
  }, 3000);
});
