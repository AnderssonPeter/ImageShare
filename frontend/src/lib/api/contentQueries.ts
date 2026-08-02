/**
 * Manual TanStack Query wrappers for the content-listing endpoints.
 *
 * Orval's `useInfinite` is disabled in `orval.config.ts`; this module
 * provides the infinite-query wrapper around the generated `getContent`
 * (root) and `getContentPath` (subfolder) functions instead.
 */

import { EntryType, type PaginatedResultOfFolderEntry } from "@lib/api/generated/imageShare.schemas";
import {
  getApiContent,
  getApiContentPath,
  type getApiContentPathResponse,
  type getApiContentResponse,
} from "@lib/api/generated/content/content";
import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { ApiError } from "@lib/api/customFetcher";

export type {
  FolderEntry,
  PaginatedResultOfFolderEntry,
} from "@lib/api/generated/imageShare.schemas";

/** Page size used for all content listing requests (backend max is 500). */
const PAGE_SIZE = 50;

/** Union of both listing response types (each is a success/error union). */
type ContentResponse = getApiContentResponse | getApiContentPathResponse;

/** Narrow a response union to its success branch and return the page data. */
function extractPageData(response: ContentResponse): PaginatedResultOfFolderEntry {
  if (response.status !== 200) {
    throw new ApiError(response.status, response.data as never);
  }
  return response.data;
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
  const params = { page, pageSize: PAGE_SIZE };
  const response: ContentResponse =
    path === undefined || path === ""
      ? await getApiContent(params, { signal })
      : await getApiContentPath(path, params, { signal });

  return extractPageData(response);
}

/** Query key factory: stable, serializable key per folder path. */
export function contentQueryKey(path: string | undefined): readonly [string, string] {
  return ["content", path ?? ""] as const;
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
  return {
    queryKey: contentQueryKey(path),
    queryFn: ({ pageParam, signal }: { pageParam: unknown; signal: AbortSignal }) =>
      fetchContentPage(path, pageParam as number, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage: PaginatedResultOfFolderEntry) => {
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

/**
 * Root-folder listing for the share-link filter builder. Fetches a single
 * large page (backend max is 500) of the root content and keeps only the
 * folder entries, returning their names.
 */
const ROOT_PAGE_SIZE = 500;

async function fetchRootFolderNames(signal: AbortSignal): Promise<string[]> {
  const response = await getApiContent({ page: 1, pageSize: ROOT_PAGE_SIZE }, { signal });
  const data = extractPageData(response);
  return data.items.filter((entry) => entry.type === EntryType.Folder).map((entry) => entry.name);
}

/** Query key for the root-folder list consumed by the share-link builder. */
export function rootFoldersQueryKey(): readonly [string] {
  return ["root-folders"] as const;
}

export function rootFoldersQueryOptions() {
  return {
    queryKey: rootFoldersQueryKey(),
    queryFn: ({ signal }: { signal: AbortSignal }) => fetchRootFolderNames(signal),
  };
}

/** Hook returning the alphabetically sorted names of all root folders. */
export function useRootFolders() {
  return useQuery({
    ...rootFoldersQueryOptions(),
    select: (names: string[]) => [...names].toSorted((left, right) => left.localeCompare(right)),
  });
}
