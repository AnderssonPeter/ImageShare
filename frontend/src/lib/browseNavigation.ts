import { useCallback } from "react";
import { useNavigate } from "@tanstack/react-router";

export interface BrowseNavigation {
  navigateFolder: (folderPath: string) => void;
  openImage: (path: string) => void;
  changeImage: (name: string) => void;
  closeImage: () => void;
}

/** Extract the filename (last path segment) from a forward-slash-delimited RelativePath. */
function filenameOf(path: string): string {
  const slash = path.lastIndexOf("/");
  return slash === -1 ? path : path.slice(slash + 1);
}

export function useBrowseNavigation(splat: string | undefined): BrowseNavigation {
  const navigate = useNavigate();
  const navigateFolder = useCallback(
    (folderPath: string) =>
      navigate({ to: "/browse/$", params: { _splat: folderPath }, search: { image: undefined } }),
    [navigate],
  );
  const openImage = useCallback(
    (openedPath: string) =>
      navigate({
        to: "/browse/$",
        params: { _splat: splat },
        search: { image: filenameOf(openedPath) },
      }),
    [navigate, splat],
  );
  const changeImage = useCallback(
    (newName: string) =>
      navigate({
        to: "/browse/$",
        params: { _splat: splat },
        search: { image: newName },
        replace: true,
      }),
    [navigate, splat],
  );
  const closeImage = useCallback(
    () =>
      navigate({
        to: "/browse/$",
        params: { _splat: splat },
        search: { image: undefined },
        replace: true,
      }),
    [navigate, splat],
  );
  return { navigateFolder, openImage, changeImage, closeImage };
}
