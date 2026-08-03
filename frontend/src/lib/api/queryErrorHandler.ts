/**
 * Global query/mutation error handling.
 *
 * The `QueryClient` (see `router.tsx`) is constructed with a `QueryCache` and
 * `MutationCache` whose `onError` delegates to `handleGlobalError`. That handler
 * turns a thrown error — typed as `ApiError` by the hey-api client interceptor
 * (`httpClient.ts`) — into one of three side effects:
 *
 *  - 401 → full-page redirect to the backend login endpoint (session expired
 *    mid-session; the backend's auth gate then re-challenges and returns the
 *    user here via `returnUrl`). A full reload is used because the backend
 *    gates all SPA access — there is no client-side login route.
 *  - 404 → ignored here so the owning component can render its own empty state
 *    from `query.error` rather than a global toast.
 *  - 403/406 (and any other unexpected error) → a `sonner` error toast so the
 *    failure is never silent.
 *
 * `resolveErrorAction` is a pure function (it takes the current path as an
 * argument instead of reading `globalThis.location`), so the branching logic
 * is unit-testable without a DOM. `handleGlobalError` performs the side effects.
 */
import { ApiError } from "@lib/api/errors";
import { toast } from "sonner";

/** Path of the backend endpoint that initiates an OIDC challenge. */
const LOGIN_ENDPOINT = "/api/authentication/login";

export type ErrorAction =
  | { readonly kind: "redirect"; readonly url: string }
  | { readonly kind: "toast"; readonly message: string }
  | { readonly kind: "ignore" };

/** Build the backend login URL with a `returnUrl` so the user lands back here. */
export function buildLoginUrl(currentPath: string): string {
  const returnUrl = currentPath === "" ? "/" : currentPath;
  return `${LOGIN_ENDPOINT}?returnUrl=${encodeURIComponent(returnUrl)}`;
}

/** Prefer the RFC 7807 detail, then its title, then the error's message. */
function problemMessage(error: ApiError): string {
  return error.problem?.detail ?? error.problem?.title ?? error.message;
}

/** Human-readable message for an arbitrary (possibly non-ApiError) error. */
function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Something went wrong.";
}

/**
 * Decide what a thrown error should do, without touching the DOM. Extracted so
 * the status-based branching can be unit-tested in isolation.
 *
 * @param error       - The error thrown by a query/mutation (an `ApiError` when
 *                      the request reached the backend, otherwise a plain
 *                      `Error` for e.g. network failure).
 * @param currentPath - The full in-app path to return to after re-login.
 */
export function resolveErrorAction(error: unknown, currentPath: string): ErrorAction {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return { kind: "redirect", url: buildLoginUrl(currentPath) };
    }
    if (error.status === 404) {
      return { kind: "ignore" };
    }
    return { kind: "toast", message: problemMessage(error) };
  }
  return { kind: "toast", message: errorMessage(error) };
}

/** Side-effecting handler bound to the query/mutation caches. */
export function handleGlobalError(error: unknown): void {
  const { location } = globalThis;
  const currentPath = location.pathname + location.search + location.hash;
  const action = resolveErrorAction(error, currentPath);
  if (action.kind === "redirect") {
    location.replace(action.url);
    return;
  }
  if (action.kind === "toast") {
    toast.error(action.message);
  }
}
