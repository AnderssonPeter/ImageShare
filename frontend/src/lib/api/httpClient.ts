/**
 * Hey API client error interceptor.
 *
 * The generated client (`generated/client.gen.ts`) parses a non-2xx response
 * body and throws it (the raw RFC 7807 problem object, or the text body). The
 * `error` interceptor registered here converts that into a typed `ApiError`
 * carrying `status` + `problem`, so React Query `error` fields and mutation
 * `onError` handlers can branch on `instanceof ApiError` and read `.status`.
 *
 * Call `registerErrorInterceptor()` once during app startup (done in
 * `main.tsx`) before the first request is issued.
 */
import { ApiError, type ProblemDetails } from "@lib/api/errors";
import { client } from "@lib/api/generated/client.gen";

/**
 * Map a thrown client error into a typed `ApiError`. Extracted as a pure
 * function so it can be unit-tested without exercising the client.
 *
 * @param error    - The parsed error body the client threw (a problem object,
 *                   a plain string, or an already-typed Error).
 * @param response - The `Response`, if one was produced (undefined on network
 *                   failure before any response).
 * @returns An `ApiError` when a response is available, otherwise the original
 *          error (or a wrapped one for non-Error primitives).
 */
export function toApiError(error: unknown, response: Response | undefined): ApiError | Error {
  if (response !== undefined) {
    const problem = isProblem(error) ? (error as ProblemDetails) : undefined;
    return new ApiError(response.status, problem);
  }
  return error instanceof Error ? error : new Error(String(error));
}

/** Whether the thrown body looks like an RFC 7807 problem details object. */
function isProblem(value: unknown): value is ProblemDetails {
  return (
    typeof value === "object" &&
    value !== null &&
    ("type" in value ||
      "title" in value ||
      "detail" in value ||
      "status" in value ||
      "instance" in value)
  );
}

/** Register the error interceptor on the singleton client. Call once at startup. */
export function registerErrorInterceptor(): void {
  client.interceptors.error.use(toApiError);
}
