import { useQuery } from "@tanstack/react-query";
import { type FolderEntry } from "@lib/api/generated";
import {
  getContentByPathOptions,
  getContentOptions,
} from "@lib/api/generated/@tanstack/react-query.gen";

function folderNamesOf(data: FolderEntry[]): string[] {
  return data
    .filter((entry) => entry.type === "Folder")
    .map((entry) => entry.name)
    .toSorted((left, right) => left.localeCompare(right));
}

export function folderContentQueryOptions(path: string | undefined) {
  const folderPath = path === undefined || path === "" ? undefined : path;
  return folderPath === undefined
    ? getContentOptions()
    : getContentByPathOptions({ path: { path: folderPath } });
}

export function useFolderContent(path?: string) {
  return useQuery(folderContentQueryOptions(path));
}

export function rootFoldersQueryOptions() {
  return {
    ...getContentOptions(),
    select: folderNamesOf,
  };
}

export function useRootFolders() {
  return useQuery(rootFoldersQueryOptions());
}
