/**
 * Custom fetch mutator for the orval-generated TanStack Query client.
 *
 * Responsibilities (see BACKEND_HANDOFF.md "Errors" & "Image serving"):
 *  - Default the `Accept` header to `application/json`; callers may override
 *    it per request via `options.headers` (e.g. an image-bytes fetch).
 *  - Unwrap RFC 7807 `application/problem+json` error bodies into a typed
 *    {@link ApiError} so React Query `onError` handlers can branch on `status`.
 *  - Parse success bodies by content type (JSON / text / blob) so the fetcher
 *    is usable for both JSON listing endpoints and any binary endpoint.
 *
 * Note: the image and download endpoints are consumed via plain URL builders
 * (`src/lib/api/urls.ts`) + `<img>`/`<a>`, NOT through this fetcher, because the
 * browser handles Accept negotiation and streaming bytes better than fetch+Q.
 * Auth cookies are same-origin (dev via the Vite proxy, prod served by the
 * backend) and are sent by default — no credentials handling needed here.
 */

/** RFC 7807 problem details. `status` may be a string per the OpenAPI schema. */
export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | string | null
  detail?: string | null
  instance?: string | null
}

/**
 * Error thrown for any non-2xx response. Carries the parsed
 * {@link ProblemDetails} when the server returned `application/problem+json`
 * (all ImageShare error responses do), so callers can read `status`, `title`,
 * and `detail` uniformly.
 */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | undefined

  constructor(status: number, problem: ProblemDetails | undefined, message?: string) {
    super(message ?? problem?.detail ?? problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

/**
 * Orval convention: exporting `ErrorType` makes the generated hooks default
 * their `TError` generic to {@link ApiError} (instead of the raw schema shape),
 * so callers get `error: ApiError` in `onError` / mutation results. The type
 * parameter is the error-shape the caller expected; it is carried only for
 * compatibility with orval's `ErrorType<...>` call sites and otherwise unused,
 * so the resolved type collapses to exactly {@link ApiError}.
 */
export type ErrorType<Error = unknown> = ApiError & Record<never, Error>

/** Orval convention: pass-through body type (no case transformation needed). */
export type BodyType<BodyData = unknown> = BodyData

/** Default headers applied to every request unless overridden per request. */
const defaultHeaders: Record<string, string> = {
  Accept: 'application/json',
}

/** Normalize a `HeadersInit` into a plain record for merging. */
function toHeaderRecord(init: HeadersInit | undefined): Record<string, string> {
  if (init instanceof Headers) {
    const record: Record<string, string> = {}
    for (const [key, value] of init.entries()) {
      record[key] = value
    }
    return record
  }
  if (Array.isArray(init)) {
    const record: Record<string, string> = {}
    for (const [key, value] of init) {
      record[key] = value
    }
    return record
  }
  return { ...init }
}

/**
 * Merge caller headers over the defaults so a per-request `Accept` (e.g.
 * `image/webp`) wins. Drops `undefined`/`null` values so they don't leak.
 */
function mergeHeaders(
  base: Record<string, string>,
  override: HeadersInit | undefined,
): Record<string, string> {
  const over = toHeaderRecord(override)
  const merged: Record<string, string> = { ...base }
  for (const [key, value] of Object.entries(over)) {
    if (value !== undefined && value !== null) {
      merged[key] = value
    }
  }
  return merged
}

/** Parse a success body based on the response content type. */
function parseBody<ResponseType>(response: Response): Promise<ResponseType> {
  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/json')) {
    return response.json() as Promise<ResponseType>
  }
  if (contentType.startsWith('text/')) {
    return response.text() as Promise<ResponseType>
  }
  // Non-JSON, non-text success (e.g. image bytes / zip) — hand back a Blob.
  return response.blob() as Promise<ResponseType>
}

/** Parse an error body, preferring RFC 7807 problem+json when present. */
async function parseProblem(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/problem+json') || contentType.includes('application/json')) {
    try {
      return (await response.json()) as ProblemDetails
    } catch {
      return undefined
    }
  }
  return undefined
}

/** Resolve a numeric HTTP status, preferring the ProblemDetails `status` field. */
function resolveStatus(response: Response, problem: ProblemDetails | undefined): number {
  if (problem?.status !== undefined && problem.status !== null) {
    const parsed = Number(problem.status)
    if (Number.isFinite(parsed)) {
      return parsed
    }
  }
  return response.status
}

/**
 * The mutator exported for orval (`mutator.name = 'customFetcher'` in
 * orval.config.ts). The generated client calls this as
 * `customFetcher<ResponseType>(url, options)` where `url` already has query
 * params baked in and `options` is a `RequestInit` with `method` (and
 * optionally the caller's `headers`/`body`/`signal`) set.
 *
 * Returns the parsed body on success; throws {@link ApiError} on any non-2xx.
 */
export const customFetcher = async <ResponseType>(
  url: string,
  options?: RequestInit,
): Promise<ResponseType> => {
  const headers = mergeHeaders(defaultHeaders, options?.headers)

  const response = await fetch(url, {
    ...options,
    headers,
  })

  if (!response.ok) {
    const problem = await parseProblem(response)
    throw new ApiError(resolveStatus(response, problem), problem)
  }

  return parseBody<ResponseType>(response)
}

export default customFetcher
