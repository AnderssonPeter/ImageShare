import { ApiError, customFetcher } from "./customFetcher";
import { describe, expect, it, vi } from "vitest";

function okJson(body: unknown, headers: Record<string, string> = {}): Response {
  return Response.json(body, {
    status: 200,
    headers: { "content-type": "application/json", ...headers },
  });
}

function problemResponse(status: number, problem: Record<string, unknown>): Response {
  return Response.json(problem, {
    status,
    headers: { "content-type": "application/problem+json" },
  });
}

const fetchMock = vi.fn<typeof fetch>();

function resetFetchMock() {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
}

/** Capture the error thrown by a promise, or undefined if it resolves. */
async function captureError(promise: Promise<unknown>): Promise<Error | undefined> {
  try {
    await promise;
    return undefined;
  } catch (error) {
    return error as Error;
  }
}

function callHeaders(index: number): Record<string, string> {
  const options = fetchMock.mock.calls[index][1] as RequestInit;
  return options.headers as Record<string, string>;
}

interface FetchResult {
  status: number;
  data: { name: string };
  headers: Headers;
}

interface BlobResult {
  status: number;
  data: Blob;
  headers: Headers;
}

describe("customFetcher JSON parsing", () => {
  it(
    "wraps a JSON success body in { status, data, headers }",
    async () => {
      expect.assertions(4);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(okJson({ name: "Jane" }));

      // Act
      const result = await customFetcher<FetchResult>("/user", { method: "GET" });

      // Assert
      expect(result.status).toBe(200);
      expect(result.data.name).toBe("Jane");
      expect(result.headers).toBeInstanceOf(Headers);
      expect(fetchMock).toHaveBeenCalledTimes(1);
    },
    1000,
  );
});

describe("customFetcher Accept header", () => {
  it(
    "defaults the Accept header to application/json",
    async () => {
      expect.assertions(1);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(okJson({}));

      // Act
      await customFetcher("/content", { method: "GET" });

      // Assert
      expect(callHeaders(0).Accept).toBe("application/json");
    },
    1000,
  );

  it(
    "lets a per-request Accept header override the default",
    async () => {
      expect.assertions(1);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(okJson({}));

      // Act
      await customFetcher("/content/image/photos", {
        method: "GET",
        headers: { Accept: "image/webp" },
      });

      // Assert
      expect(callHeaders(0).Accept).toBe("image/webp");
    },
    1000,
  );
});

describe("customFetcher problem+json error", () => {
  it(
    "unwraps a problem+json error into an ApiError with the problem status",
    async () => {
      expect.assertions(4);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(
        problemResponse(404, { status: 404, title: "Not Found", detail: "folder missing" }),
      );

      // Act
      const thrown = await captureError(
        customFetcher("/content/missing", { method: "GET" }),
      );

      // Assert
      expect(thrown).toBeInstanceOf(ApiError);
      expect((thrown as ApiError).status).toBe(404);
      expect((thrown as ApiError).problem?.title).toBe("Not Found");
      expect((thrown as ApiError).message).toBe("folder missing");
    },
    1000,
  );
});

describe("customFetcher fallback status", () => {
  it(
    "falls back to the HTTP response status when the problem body has no status",
    async () => {
      expect.assertions(1);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(problemResponse(403, { title: "Forbidden" }));

      // Act + Assert
      await expect(customFetcher("/content/secret", { method: "GET" })).rejects.toMatchObject({
        status: 403,
      });
    },
    1000,
  );
});

describe("customFetcher binary", () => {
  it(
    "wraps a non-JSON success response as a Blob in data",
    async () => {
      expect.assertions(3);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(
        new Response(new Uint8Array([1, 2, 3]), {
          status: 200,
          headers: { "content-type": "application/zip" },
        }),
      );

      // Act
      const result = await customFetcher<BlobResult>("/content/download/photos", {
        method: "GET",
      });

      // Assert
      expect(result.status).toBe(200);
      expect(result.data).toBeInstanceOf(Blob);
      expect(result.headers).toBeInstanceOf(Headers);
    },
    1000,
  );
});

describe("customFetcher signal", () => {
  it(
    "forwards the abort signal and method to fetch",
    async () => {
      expect.assertions(2);
      // Arrange
      resetFetchMock();
      fetchMock.mockResolvedValueOnce(okJson({}));
      const controller = new AbortController();

      // Act
      await customFetcher("/user", { method: "GET", signal: controller.signal });

      // Assert
      expect(fetchMock.mock.calls[0][1]?.method).toBe("GET");
      expect(fetchMock.mock.calls[0][1]?.signal).toBe(controller.signal);
    },
    1000,
  );
});
