/**
 * Runtime guards + `ensure` helpers for hey-api SDK response bodies.
 *
 * The generated `*Responses` types are `type` aliases (the oxlint override for
 * `src/lib/api/generated/**` disables `consistent-type-definitions`, which
 * would otherwise rewrite them as `interface`s and break the client's response
 * narrowing), so the SDK `data` is already typed as the narrowed success body.
 * These `ensure*` helpers add a defensive runtime shape check on top of the
 * static type, so a malformed backend response surfaces as a `TypeError`
 * (propagated to React Query's error channel and the global `onError` handler)
 * rather than a confusing property-access failure downstream.
 */
import { type User } from "@lib/api/generated";

/** Whether `value` is a non-null, non-array object. */
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** Guard: `value` is a `User` (all fields optional, so just an object). */
export function isUser(value: unknown): value is User {
  return isRecord(value);
}

/** Ensure `value` is a `User` object, throwing a `TypeError` otherwise. */
export function ensureUser(value: unknown): User {
  if (!isUser(value)) {
    throw new TypeError("Expected a User object");
  }
  return value;
}
