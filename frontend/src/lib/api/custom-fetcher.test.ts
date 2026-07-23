import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, customFetcher } from "./custom-fetcher";

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

/** Read the headers record from the Nth captured fetch call (typed for tests). */
function callHeaders(index: number): Record<string, string> {
  const options = fetchMock.mock.calls[index][1] as RequestInit;
  return options.headers as Record<string, string>;
}

describe("customFetcher JSON parsing", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it(
    "parses a JSON success body",
    async () => {
      expect.assertions(2);
      // Arrange
      fetchMock.mockResolvedValueOnce(okJson({ isAuthenticated: true, isAdmin: false, name: "Jane" }));

      // Act
      const result = await customFetcher<{ name: string }>("/user", { method: "GET" });

      // Assert
      expect(result.name).toBe("Jane");
      expect(fetchMock).toHaveBeenCalledOnce();
    },
    1000,
  );
});

describe("customFetcher Accept header", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it(
    "defaults the Accept header to application/json",
    async () => {
      expect.assertions(1);
      // Arrange
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

describe("customFetcher error handling", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it(
    "unwraps a problem+json error into an ApiError with the problem status",
    async () => {
      expect.assertions(4);
      // Arrange
      fetchMock.mockResolvedValueOnce(
        problemResponse(404, { status: 404, title: "Not Found", detail: "folder missing" }),
      );

      // Act
      let thrown: unknown;
      try {
        await customFetcher("/content/missing", { method: "GET" });
      } catch (error) {
        thrown = error;
      }

      // Assert
      expect(thrown).toBeInstanceOf(ApiError);
      expect((thrown as ApiError).status).toBe(404);
      expect((thrown as ApiError).problem?.title).toBe("Not Found");
      expect((thrown as ApiError).message).toBe("folder missing");
    },
    1000,
  );

  it(
    "falls back to the HTTP response status when the problem body has no status",
    async () => {
      expect.assertions(1);
      // Arrange
      fetchMock.mockResolvedValueOnce(problemResponse(403, { title: "Forbidden" }));

      // Act + Assert
      await expect(customFetcher("/content/secret", { method: "GET" })).rejects.toMatchObject({
        status: 403,
      });
    },
    1000,
  );
});

describe("customFetcher binary and signal", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it(
    "returns a Blob for non-JSON success responses",
    async () => {
      expect.assertions(1);
      // Arrange
      fetchMock.mockResolvedValueOnce(
        new Response(new Uint8Array([1, 2, 3]), {
          status: 200,
          headers: { "content-type": "application/zip" },
        }),
      );

      // Act
      const result = await customFetcher<Blob>("/content/download/photos", { method: "GET" });

      // Assert
      expect(result).toBeInstanceOf(Blob);
    },
    1000,
  );

  it(
    "forwards the abort signal and method to fetch",
    async () => {
      expect.assertions(2);
      // Arrange
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
