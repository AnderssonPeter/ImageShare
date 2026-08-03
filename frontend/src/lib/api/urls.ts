/**
 * Plain URL builders for the image, random, download, and share-login endpoints.
 *
 * These endpoints serve binary content (image bytes or a zip stream) or are
 * consumed outside the fetch mutator (`<img src>` / `<a href>` / QR code), so
 * the browser handles Accept negotiation and streaming bytes better than fetch.
 *
 * Path segments are URL-encoded per BACKEND_HANDOFF.md: a `RelativePath` is
 * relative, forward-slash-delimited, and must be encoded when used as a path
 * parameter (e.g. `photos/2024` → `photos%2F2024`).
 */

/** Encode a RelativePath for use as a single path parameter segment. */
function encodePath(path: string): string {
  return encodeURIComponent(path);
}

/**
 * Build the URL for `GET /content/image/{path}`.
 *
 * @param path     - RelativePath of the image file.
 * @param thumbnail - When true, request the 200×200 thumbnail variant.
 * @returns A URL string suitable for `<img src>`.
 */
export function imageUrl(path: string, thumbnail: boolean): string {
  const params = new URLSearchParams();
  if (thumbnail) {
    params.set("thumbnail", "true");
  }
  const query = params.toString();
  return query
    ? `/api/content/image/${encodePath(path)}?${query}`
    : `/api/content/image/${encodePath(path)}`;
}

/**
 * Build the URL for `GET /content/random` — a random image from the root
 * folder (recursive), used as the grid background when no folder is open.
 *
 * @param thumbnail - When true, request the thumbnail variant.
 * @returns A URL string suitable for `<img src>` or CSS `background-image`.
 */
export function randomRootUrl(thumbnail: boolean): string {
  const params = new URLSearchParams();
  if (thumbnail) {
    params.set("thumbnail", "true");
  }
  const query = params.toString();
  return query ? `/api/content/random?${query}` : "/api/content/random";
}

/**
 * Build the URL for `GET /content/random/{folder}` — a random image from a
 * folder, used as a cover image for folder tiles.
 *
 * @param folder    - RelativePath of the folder to pick from.
 * @param thumbnail - When true, request the thumbnail variant.
 * @param recursive - When true, pick from the folder recursively (all subfolders).
 * @returns A URL string suitable for `<img src>`.
 */
export function randomFolderUrl(folder: string, thumbnail: boolean, recursive: boolean): string {
  const params = new URLSearchParams();
  if (thumbnail) {
    params.set("thumbnail", "true");
  }
  if (recursive) {
    params.set("recursive", "true");
  }
  const query = params.toString();
  return query
    ? `/api/content/random/${encodePath(folder)}?${query}`
    : `/api/content/random/${encodePath(folder)}`;
}

/**
 * Build the URL for `GET /content/download/{folder}` — a zip download of all
 * images in a folder matching the given formats.
 *
 * @param folder   - RelativePath of the folder to download.
 * @param formats  - Image formats to include (e.g. `['avif', 'webp']`). Repeated
 *                   `format` query params are produced.
 * @returns A URL string suitable for `<a href>`.
 */
export function downloadUrl(folder: string, formats: readonly string[]): string {
  const params = new URLSearchParams();
  for (const format of formats) {
    params.append("format", format);
  }
  const query = params.toString();
  return query
    ? `/api/content/download/${encodePath(folder)}?${query}`
    : `/api/content/download/${encodePath(folder)}`;
}

/**
 * Build the absolute shareable sign-in URL for `GET /authentication/login/jwt/{token}`.
 *
 * Unlike the other builders (which return relative paths for `<img src>` /
 * `<a href>`), this returns an absolute URL because it is encoded into a QR
 * code and copied to the clipboard — both need an origin the recipient's device
 * can reach.
 *
 * @param token  - The JWT string returned by the token-generation endpoint.
 * @param origin - The current page origin (e.g. `https://imageshare.example`).
 * @returns An absolute sign-in URL.
 */
export function buildShareUrl(token: string, origin: string): string {
  return `${origin}/api/authentication/login/jwt/${token}`;
}
