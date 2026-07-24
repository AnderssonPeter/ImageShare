/**
 * Manual TanStack Query wrappers for the content-listing endpoints.
 *
 * Orval's `useInfinite` is disabled in `orval.config.ts`; this module
 * provides the infinite-query wrapper around the generated `getContent`
 * (root) and `getContentPath` (subfolder) functions instead.
 */

import {
  getContent,
  getContentPath,
  type getContentPathResponse,
  type getContentResponse,
} from './generated/content/content'
import { ApiError } from './custom-fetcher'
import { type PaginatedResultOfFolderEntry } from './generated/imageShare.schemas'
import { useInfiniteQuery } from '@tanstack/react-query'

export type { FolderEntry, PaginatedResultOfFolderEntry } from './generated/imageShare.schemas'

/** Page size used for all content listing requests (backend max is 500). */
const PAGE_SIZE = 50

/** Union of both listing response types (each is a success/error union). */
type ContentResponse = getContentResponse | getContentPathResponse

/** Narrow a response union to its success branch and return the page data. */
function extractPageData(response: ContentResponse): PaginatedResultOfFolderEntry {
  if (response.status !== 200) {
    throw new ApiError(response.status, response.data as never)
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

/** Fetch a single page of folder content. */
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
 * - No `path` (or empty) -> `GET /content?page=N&pageSize=50` (root listing).
 * - With `path`          -> `GET /content/{path}?page=N&pageSize=50` (subfolder).
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

      return loadedCount < totalCount ? currentPage + 1 : undefined
    },
  })
}
