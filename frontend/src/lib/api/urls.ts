/**
 * Plain URL builders for the image, random, and download endpoints.
 *
 * These endpoints serve binary content (image bytes or a zip stream), so they
 * are consumed via `<img src>` / `<a href>` rather than the fetch mutator — the
 * browser handles Accept negotiation and streaming bytes better than fetch.
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
    ? `/content/image/${encodePath(path)}?${query}`
    : `/content/image/${encodePath(path)}`;
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
    ? `/content/random/${encodePath(folder)}?${query}`
    : `/content/random/${encodePath(folder)}`;
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
    ? `/content/download/${encodePath(folder)}?${query}`
    : `/content/download/${encodePath(folder)}`;
}
