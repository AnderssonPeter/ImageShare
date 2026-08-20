/**
 * Persists the authenticated user's `AuthenticationSource` in a long-lived
 * cookie so it survives a session expiry.
 *
 * The user endpoint (`GET /api/authentication/user`) only returns the source
 * while the session is alive — once it 401s we can no longer ask the backend
 * how the user originally signed in. Storing it client-side at the time of the
 * last successful fetch lets the global error handler branch a later 401 by
 * source (OpenIdConnect → re-login toast, Jwt → no-access toast, ApiKey →
 * impossible, see `queryErrorHandler`).
 *
 * "Lasts forever" is approximated with a 10-year `max-age` (well beyond any
 * realistic session lifetime); the cookie is scoped to the whole site (`/`).
 */
import { type AuthenticationSource } from "@lib/api/generated";

/** Cookie name holding the most recent successful `AuthenticationSource`. */
const AUTH_SOURCE_COOKIE = "authSource";

/** 10 years in seconds — long enough to be effectively permanent. */
const MAX_AGE_SECONDS = 315_360_000;

const VALID_SOURCES: readonly AuthenticationSource[] = ["None", "OpenIdConnect", "ApiKey", "Jwt"];

function isAuthenticationSource(value: string): value is AuthenticationSource {
  return (VALID_SOURCES as readonly string[]).includes(value);
}

/** Safely reach `document.cookie` (absent in Node/SSR and some test setups). */
function getDocumentCookie(): string | undefined {
  try {
    return typeof globalThis === "undefined" ? undefined : globalThis.document.cookie;
  } catch (error) {
    console.warn("Unable to read document.cookie:", error);
    return undefined;
  }
}

/**
 * Read the previously stored `AuthenticationSource`, or `undefined` when no
 * valid cookie is present (first visit, or the cookie was cleared).
 */
export function getStoredAuthSource(): AuthenticationSource | undefined {
  const cookie = getDocumentCookie();
  if (cookie === undefined) {
    return undefined;
  }
  for (const part of cookie.split(";")) {
    const [name, ...valueParts] = part.trim().split("=");
    if (name === AUTH_SOURCE_COOKIE) {
      const value = valueParts.join("=");
      return isAuthenticationSource(value) ? value : undefined;
    }
  }
  return undefined;
}

/**
 * Persist `source` for 10 years so a subsequent session-expiry 401 can branch
 * on it. Writing is best-effort: if cookies are unavailable this is a no-op.
 */
export function storeAuthSource(source: AuthenticationSource): void {
  try {
    globalThis.document.cookie = `${AUTH_SOURCE_COOKIE}=${source}; path=/; max-age=${MAX_AGE_SECONDS}; samesite=lax`;
  } catch (error) {
    console.warn("Unable to write document.cookie:", error);
    // Cookies unavailable (SSR / restricted context) — nothing to do.
  }
}
