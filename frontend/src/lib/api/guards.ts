/**
 * Runtime guards + `ensure` helpers for hey-api SDK response bodies.
 *
 * hey-api types each SDK function's `data` as the whole `*Responses`
 * object (e.g. `GetCurrentUserResponses` = `{ 200: User }`) instead of the
 * narrowed success body, because the generated `*Responses` interfaces do
 * not extend `Record<string, unknown>` and so hey-api's
 * `TData extends Record<string, unknown>` conditional falls through. At
 * runtime the client returns the parsed body directly, so these helpers
 * narrow it back *with a runtime shape check* rather than an unchecked
 * `as unknown as` cast.
 *
 * Each `ensure*` helper throws a `TypeError` when the value does not match
 * the expected shape; callers in query/mutation functions propagate that
 * to React Query's error channel (and the global `onError` handler).
 */
import {
  type FolderEntry,
  type PaginatedResultOfFolderEntry,
  type User,
} from "@lib/api/generated";

/** Whether `value` is a non-null, non-array object. */
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** Whether `value` is the API's `number | string` coerced field. */
function isNumberOrString(value: unknown): value is number | string {
  return typeof value === "number" || typeof value === "string";
}

/** Guard: `value` is a `FolderEntry`. */
function isFolderEntry(value: unknown): value is FolderEntry {
  if (!isRecord(value)) {
    return false;
  }
  return (
    typeof value.name === "string" &&
    typeof value.path === "string" &&
    (value.type === "Folder" || value.type === "File")
  );
}

/** Guard: `value` is a `PaginatedResultOfFolderEntry`. */
export function isPaginatedResultOfFolderEntry(
  value: unknown,
): value is PaginatedResultOfFolderEntry {
  if (!isRecord(value)) {
    return false;
  }
  return (
    Array.isArray(value.items) &&
    value.items.every(isFolderEntry) &&
    isNumberOrString(value.page) &&
    isNumberOrString(value.pageSize) &&
    isNumberOrString(value.totalCount)
  );
}

/** Guard: `value` is a `User` (all fields optional, so just an object). */
export function isUser(value: unknown): value is User {
  return isRecord(value);
}

/**
 * Ensure `value` is a `PaginatedResultOfFolderEntry`, throwing a `TypeError`
 * otherwise. Delegates the structural check to the guard so the `ensure`
 * helper stays a thin assertion.
 */
export function ensurePage(value: unknown): PaginatedResultOfFolderEntry {
  if (!isPaginatedResultOfFolderEntry(value)) {
    throw new TypeError("Expected a PaginatedResultOfFolderEntry");
  }
  return value;
}

/** Ensure `value` is a `User` object, throwing a `TypeError` otherwise. */
export function ensureUser(value: unknown): User {
  if (!isUser(value)) {
    throw new TypeError("Expected a User object");
  }
  return value;
}

/** Ensure `value` is a `string`, throwing a `TypeError` otherwise. */
export function ensureString(value: unknown): string {
  if (typeof value !== "string") {
    throw new TypeError("Expected a string");
  }
  return value;
}
