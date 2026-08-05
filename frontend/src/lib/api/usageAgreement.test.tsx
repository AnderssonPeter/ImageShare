import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  type UsageAgreementResponse,
  type acceptUsageAgreement,
  type getUsageAgreement,
} from "@lib/api/generated";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useAcceptUsageAgreement, useUsageAgreementQuery } from "@lib/api/usageAgreement";
import { ApiError } from "@lib/api/errors";
import { type ReactNode } from "react";

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
  text: "You agree to use this service responsibly.",
  accepted: false,
};

function sdkResponse<TData = undefined>(data?: TData) {
  return {
    data,
    request: new Request("http://localhost/api/usage-agreement"),
    response: new Response(),
  } as never;
}

function renderWithClient<THook>(hook: () => THook) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  return renderHook(hook, { wrapper: Wrapper });
}

function setupAcceptMocks() {
  mockAcceptUsageAgreement.mockReset();
  mockGetUsageAgreement.mockReset();
  mockAcceptUsageAgreement.mockResolvedValueOnce(sdkResponse());
  mockGetUsageAgreement
    .mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: false }))
    .mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: true }));
}

describe("usage agreement query", () => {
  it("exposes the agreement when the feature is enabled and accepted is false", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockResolvedValueOnce(sdkResponse({ ...AGREEMENT, accepted: false }));

    // Act
    const { result } = renderWithClient(() => useUsageAgreementQuery());
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    // Assert
    expect(result.current.data).toStrictEqual({ ...AGREEMENT, accepted: false });
  }, 2000);

  it("reports the feature as disabled when the endpoint returns 404", async () => {
    expect.hasAssertions();
    // Arrange
    mockGetUsageAgreement.mockReset();
    mockGetUsageAgreement.mockRejectedValueOnce(new ApiError(404, undefined));

    // Act
    const { result } = renderWithClient(() => useUsageAgreementQuery());
    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });

    // Assert
    expect(result.current.error).toBeInstanceOf(ApiError);
  }, 2000);
});

describe("accept usage agreement", () => {
  it("posts to the accept endpoint and invalidates the agreement query", async () => {
    expect.hasAssertions();
    // Arrange
    setupAcceptMocks();

    // Act
    const { result } = renderWithClient(() => {
      const query = useUsageAgreementQuery();
      const accept = useAcceptUsageAgreement();
      return { query, accept };
    });
    await waitFor(() => {
      expect(result.current.query.isSuccess).toBe(true);
    });
    result.current.accept.mutate({});
    await waitFor(() => {
      expect(mockAcceptUsageAgreement).toHaveBeenCalledTimes(1);
    });

    // Assert
    await waitFor(() => {
      expect(result.current.query.data?.accepted).toBe(true);
    });
  }, 3000);
});
