/**
 * API error types.
 *
 * `ApiError` is thrown for any non-2xx response by the hey-api client error
 * interceptor (see `httpClient.ts`), so React Query `error` fields and
 * mutation `onError` handlers carry a typed `status` + RFC 7807
 * `problem`. The shape mirrors the backend `ProblemDetails` schema.
 */

/** RFC 7807 problem details. `status` may be a string per the OpenAPI schema. */
export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | string | null;
  detail?: string | null;
  instance?: string | null;
}

/** Error thrown for any non-2xx response. Carries parsed ProblemDetails. */
export class ApiError extends Error {
  public readonly status: number;
  public readonly problem: ProblemDetails | undefined;

  public constructor(status: number, problem: ProblemDetails | undefined, message?: string) {
    super(message ?? problem?.detail ?? problem?.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}
