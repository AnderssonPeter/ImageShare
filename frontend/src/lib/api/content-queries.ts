/**
 * Manual TanStack Query wrappers for the content-listing endpoints.
 *
 * Orval's `useInfinite` is disabled in `orval.config.ts`; this module
 * provides the infinite-query wrapper around the generated `getContent`
 * (root) and `getContentPath` (subfolder) functions instead.
 */

import { useInfiniteQuery } from '@tanstack/react-query'

import { ApiError, type ProblemDetails } from './custom-fetcher'
import {
  getContent,
  getContentPath,
  type FolderEntry,
  type PaginatedResultOfFolderEntry,
  type getContentResponse,
  type getContentPathResponse,
} from './generated/imageShare'

/** Page size used for all content listing requests (backend max is 500). */
const PAGE_SIZE = 50

/** Union of both listing response types (each is a success/error union). */
type ContentResponse = getContentResponse | getContentPathResponse

/**
 * Narrow a response union to its success branch and return the
 * {@link PaginatedResultOfFolderEntry} payload.
 *
 * The custom fetcher throws {@link ApiError} on non-2xx, so the error branch is
 * unreachable at runtime — this guard exists solely for TypeScript to narrow
 * the union so callers get a typed `PaginatedResultOfFolderEntry`.
 */
function extractPageData(response: ContentResponse): PaginatedResultOfFolderEntry {
  if (response.status !== 200) {
    throw new ApiError(response.status, response.data as ProblemDetails)
  }
  return response.data
}

/** Coerce a `number | string` field from the API into a finite number. */
function toNumber(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(parsed)) {
    throw new TypeError(`Expected a finite number, got: ${value}`)
  }
  return parsed
}

/**
 * Fetch a single page of folder content, routing to the correct endpoint based
 * on whether `path` is provided. Both endpoints use the same lowercase
 * `page`/`pageSize` params.
 */
async function fetchContentPage(
  path: string | undefined,
  page: number,
  signal: AbortSignal,
): Promise<PaginatedResultOfFolderEntry> {
  const params = { page, pageSize: PAGE_SIZE }
  const response: ContentResponse = path === undefined || path === ''
    ? await getContent(params, { signal })
    : await getContentPath(path, params, { signal })

  return extractPageData(response)
}

/** Query key factory: stable, serializable key per folder path. */
function contentQueryKey(path: string | undefined): readonly [string, string] {
  return ['content', path ?? ''] as const
}

/**
 * Infinite-query hook for browsing folder content.
 *
 * - No `path` (or empty) → `GET /content?page=N&pageSize=50` (root listing).
 * - With `path`           → `GET /content/{path}?page=N&pageSize=50` (subfolder).
 *
 * Returns the raw `useInfiniteQuery` result; each page in `data.pages` is a
 * {@link PaginatedResultOfFolderEntry}. Flatten with
 * `data?.pages.flatMap(page => page.items)` to get a flat item list.
 */
export function useFolderContent(path?: string) {
  return useInfiniteQuery({
    queryKey: contentQueryKey(path),
    queryFn: ({ pageParam, signal }) => fetchContentPage(path, pageParam as number, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const currentPage = toNumber(lastPage.page)
      const totalCount = toNumber(lastPage.totalCount)
      const loadedCount = toNumber(lastPage.pageSize) * currentPage

      if (loadedCount < totalCount) {
        return currentPage + 1
      }
      return undefined
    },
  })
}

export type { FolderEntry, PaginatedResultOfFolderEntry }
