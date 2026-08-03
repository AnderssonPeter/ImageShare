/**
 * Manual TanStack Query wrappers for the content-listing endpoints.
 *
 * hey-api generates infinite-query options, but a typing quirk (the response
 * `*Responses` interfaces don't extend `Record<string, unknown>`, so the SDK
 * `data` is typed as the whole `{ 200: ... }` object instead of the narrowed
 * response) leaks when the generated options are spread. To keep things robust
 * we build the query options manually with plain serializable query keys and a
 * single cast from the SDK `data` to `PaginatedResultOfFolderEntry` (at
 * runtime the body is the page object directly). Errors throw `ApiError`
 * (mapped by the hey-api client error interceptor in `httpClient.ts`).
 */
import {
  type PaginatedResultOfFolderEntry,
  getContent,
  getContentByPath,
} from "@lib/api/generated";
import { useInfiniteQuery, useQuery } from "@tanstack/react-query";

export type { FolderEntry, PaginatedResultOfFolderEntry } from "@lib/api/generated";

/** Page size used for all content listing requests (backend max is 500). */
const PAGE_SIZE = 50;

/** Single large page for the root-folder list consumed by the share builder. */
const ROOT_PAGE_SIZE = 500;

/**
 * The SDK types `data` as the whole `*Responses` object (see module note); at
 * runtime it is the page object directly, so this cast narrows it back.
 */
function asPage(data: unknown): PaginatedResultOfFolderEntry {
  return data as unknown as PaginatedResultOfFolderEntry;
}

/** Coerce a `number | string` field from the API into a finite number. */
function toNumber(value: number | string): number {
  const parsed = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(parsed)) {
    throw new TypeError(`Expected a finite number, got: ${value}`);
  }
  return parsed;
}

/** Fetch a single page of folder content. */
async function fetchContentPage(
  path: string | undefined,
  page: number,
  signal: AbortSignal,
): Promise<PaginatedResultOfFolderEntry> {
  const result =
    path === undefined
      ? await getContent({ query: { page, pageSize: PAGE_SIZE }, signal })
      : await getContentByPath({ path: { path }, query: { page, pageSize: PAGE_SIZE }, signal });
  return asPage(result.data);
}

/**
 * Infinite-query options for browsing folder content, shared between the
 * `useFolderContent` hook and route loaders (so a loader can prefetch the
 * first page into the same cache the hook reads).
 *
 * - No `path` (or empty) -> `GET /content?page=N&pageSize=50` (root listing).
 * - With `path`          -> `GET /content/{path}?page=N&pageSize=50` (subfolder).
 */
export function folderContentQueryOptions(path: string | undefined) {
  const folderPath = path === undefined || path === "" ? undefined : path;
  return {
    queryKey: ["content", folderPath ?? ""] as const,
    queryFn: ({ pageParam, signal }: { pageParam: number; signal: AbortSignal }) =>
      fetchContentPage(folderPath, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage: PaginatedResultOfFolderEntry): number | undefined => {
      const currentPage = toNumber(lastPage.page);
      const totalCount = toNumber(lastPage.totalCount);
      const loadedCount = toNumber(lastPage.pageSize) * currentPage;
      return loadedCount < totalCount ? currentPage + 1 : undefined;
    },
  };
}

/**
 * Infinite-query hook for browsing folder content.
 *
 * - No `path` (or empty) -> `GET /content?page=N&pageSize=50` (root listing).
 * - With `path`          -> `GET /content/{path}?page=N&pageSize=50` (subfolder).
 */
export function useFolderContent(path?: string) {
  return useInfiniteQuery(folderContentQueryOptions(path));
}

async function fetchRootFolderNames(signal: AbortSignal): Promise<string[]> {
  const result = await getContent({ query: { page: 1, pageSize: ROOT_PAGE_SIZE }, signal });
  const data = asPage(result.data);
  return data.items
    .filter((entry) => entry.type === "Folder")
    .map((entry) => entry.name)
    .toSorted((left, right) => left.localeCompare(right));
}

/** Query options for the root-folder list consumed by the share-link builder. */
export function rootFoldersQueryOptions() {
  return {
    queryKey: ["root-folders"] as const,
    queryFn: ({ signal }: { signal: AbortSignal }) => fetchRootFolderNames(signal),
  };
}

/** Hook returning the alphabetically sorted names of all root folders. */
export function useRootFolders() {
  return useQuery(rootFoldersQueryOptions());
}
