import {
  type InfiniteData,
  type UseInfiniteQueryOptions,
  useInfiniteQuery,
  useQuery,
} from "@tanstack/react-query";
import { type PaginatedResultOfFolderEntry, type ProblemDetails } from "@lib/api/generated";
import {
  getContentByPathInfiniteOptions,
  getContentInfiniteOptions,
  getContentOptions,
} from "@lib/api/generated/@tanstack/react-query.gen";

const PAGE_SIZE = 50;
const ROOT_PAGE_SIZE = 500;

function toNumber(value: number | string): number {
  const parsed = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(parsed)) {
    throw new TypeError(`Expected a finite number, got: ${value}`);
  }
  return parsed;
}

function nextContentPage(lastPage: PaginatedResultOfFolderEntry): number | undefined {
  const currentPage = toNumber(lastPage.page);
  const totalCount = toNumber(lastPage.totalCount);
  const loadedCount = toNumber(lastPage.pageSize) * currentPage;
  return loadedCount < totalCount ? currentPage + 1 : undefined;
}

function folderNamesOf(data: PaginatedResultOfFolderEntry): string[] {
  return data.items
    .filter((entry) => entry.type === "Folder")
    .map((entry) => entry.name)
    .toSorted((left, right) => left.localeCompare(right));
}

type FolderContentOptions = UseInfiniteQueryOptions<
  PaginatedResultOfFolderEntry,
  ProblemDetails,
  InfiniteData<PaginatedResultOfFolderEntry>,
  readonly unknown[],
  number
>;

export function folderContentQueryOptions(path: string | undefined): FolderContentOptions {
  const folderPath = path === undefined || path === "" ? undefined : path;
  const base =
    folderPath === undefined
      ? getContentInfiniteOptions({ query: { pageSize: PAGE_SIZE } })
      : getContentByPathInfiniteOptions({
          path: { path: folderPath },
          query: { pageSize: PAGE_SIZE },
        });
  return {
    ...base,
    initialPageParam: 1,
    getNextPageParam: nextContentPage,
  } as FolderContentOptions;
}

export function useFolderContent(path?: string) {
  return useInfiniteQuery(folderContentQueryOptions(path));
}

export function rootFoldersQueryOptions() {
  return {
    ...getContentOptions({ query: { page: 1, pageSize: ROOT_PAGE_SIZE } }),
    select: folderNamesOf,
  };
}

export function useRootFolders() {
  return useQuery(rootFoldersQueryOptions());
}
