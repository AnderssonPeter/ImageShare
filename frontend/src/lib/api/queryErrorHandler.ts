/**
 * Global query/mutation error handling.
 *
 * The `QueryClient` (see `router.tsx`) is constructed with a `QueryCache` and
 * `MutationCache` whose `onError` delegates to `handleGlobalError`. That handler
 * turns a thrown error — typed as `ApiError` by the hey-api client interceptor
 * (`httpClient.ts`) — into one of several side effects:
 *
 *  - 401 (session expired mid-session) — branched by the last known
 *    `AuthenticationSource` (read from the `authSource` cookie, see
 *    `authSource.ts`, since the user endpoint itself now 401s):
 *      • OpenIdConnect → an error toast "session expired" with a "Sign in"
 *        action button that initiates a fresh OIDC challenge (full-page
 *        redirect to the backend login endpoint with a `returnUrl`). Not
 *        auto-redirected so the user isn't yanked away mid-action.
 *      • Jwt → an error toast "you no longer have access" (the token was
 *        revoked/expired and there is no re-login flow for share links).
 *      • ApiKey → ignored; an API key cannot expire mid-session.
 *      • unknown / `None` → fall back to a full-page redirect to the login
 *        endpoint (the safe pre-source behaviour, used before the cookie is set).
 *  - 404 → ignored here so the owning component can render its own empty state
 *    from `query.error` rather than a global toast.
 *  - 403/406 (and any other unexpected error) → a `sonner` error toast so the
 *    failure is never silent.
 *
 * `resolveErrorAction` is a pure function (it takes the current path and the
 * auth source as arguments instead of reading globals), so the branching logic
 * is unit-testable without a DOM. `handleGlobalError` performs the side effects
 * — including a one-shot guard that suppresses repeated 401 toasts once a
 * session-expiry has been surfaced (after expiry every observed query will 401
 * on its next refetch; surfacing each one would spam the user).
 */
import { type AuthenticationSource } from "@lib/api/generated";
import { ApiError } from "@lib/api/errors";
import { type Translate } from "@lib/i18n";
import { getStoredAuthSource } from "@lib/authSource";
import { toast } from "sonner";

/** Path of the backend endpoint that initiates an OIDC challenge. */
const LOGIN_ENDPOINT = "/api/authentication/login";

/** A primary action offered on a toast (e.g. the "Sign in" button). */
export interface ToastAction {
  readonly label: string;
  readonly url: string;
}

export type ErrorAction =
  | { readonly kind: "redirect"; readonly url: string }
  | { readonly kind: "toast"; readonly message: string; readonly action?: ToastAction }
  | { readonly kind: "ignore" };

/** Inputs to `resolveErrorAction`, passed as one object to stay under the
 *  three-parameter lint limit while keeping the function pure (no globals). */
export interface ErrorContext {
  /** Full in-app path (pathname + search + hash) to return to after re-login. */
  readonly currentPath: string;
  /** Active translate function (re-read each call so language changes apply). */
  readonly translate: Translate;
  /** Last known auth source; defaults to the `authSource` cookie value. */
  readonly authSource?: AuthenticationSource;
}

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
function errorMessage(error: unknown, translate: Translate): string {
  return error instanceof Error ? error.message : translate("errors.unknown");
}

/**
 * Decide how a 401 should surface, based on the last known auth source.
 *
 * @param authSource - The `AuthenticationSource` from the `authSource` cookie
 *                      (or `undefined` when none is stored yet).
 * @param loginUrl   - Login endpoint URL (with `returnUrl`) for the OIDC case.
 */
function resolveUnauthenticated(
  authSource: AuthenticationSource | undefined,
  loginUrl: string,
  translate: Translate,
): ErrorAction {
  switch (authSource) {
    case "OpenIdConnect": {
      return {
        kind: "toast",
        message: translate("errors.sessionExpired"),
        action: { label: translate("errors.signIn"), url: loginUrl },
      };
    }
    case "Jwt": {
      return { kind: "toast", message: translate("errors.noAccess") };
    }
    case "ApiKey":
    case "None": {
      return { kind: "ignore" };
    }
    case undefined: {
      return { kind: "redirect", url: loginUrl };
    }
  }
}

/**
 * Decide what a thrown error should do, without touching the DOM. Extracted so
 * the status-based branching can be unit-tested in isolation.
 */
export function resolveErrorAction(error: unknown, context: ErrorContext): ErrorAction {
  const { currentPath, translate, authSource = getStoredAuthSource() } = context;
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return resolveUnauthenticated(authSource, buildLoginUrl(currentPath), translate);
    }
    if (error.status === 404) {
      return { kind: "ignore" };
    }
    return { kind: "toast", message: problemMessage(error) };
  }
  return { kind: "toast", message: errorMessage(error, translate) };
}

/**
 * Whether a session-expiry 401 toast has already been surfaced this page load.
 * Once true, further 401 toasts are suppressed — after expiry every observed
 * query refetches and 401s, and surfacing each one would spam the user. A full
 * reload (e.g. the "Sign in" button → OIDC challenge → return) resets it.
 */
const sessionExpiryState = { notified: false };

/** Fire a `sonner` toast for a `toast` action, applying the 401 one-shot guard. */
function fireToast(action: Extract<ErrorAction, { kind: "toast" }>, isAuthExpiry: boolean): void {
  if (isAuthExpiry) {
    if (sessionExpiryState.notified) {
      return;
    }
    sessionExpiryState.notified = true;
  }
  if (action.action === undefined) {
    toast.error(action.message);
    return;
  }
  const { label, url } = action.action;
  toast.error(action.message, {
    action: {
      label,
      onClick: () => globalThis.location.assign(url),
    },
  });
}

/** Side-effecting handler bound to the query/mutation caches. */
export function handleGlobalError(error: unknown, translate: Translate): void {
  const { location } = globalThis;
  const currentPath = location.pathname + location.search + location.hash;
  const action = resolveErrorAction(error, { currentPath, translate });
  if (action.kind === "redirect") {
    location.replace(action.url);
    return;
  }
  if (action.kind === "toast") {
    const isAuthExpiry = error instanceof ApiError && error.status === 401;
    fireToast(action, isAuthExpiry);
  }
}
