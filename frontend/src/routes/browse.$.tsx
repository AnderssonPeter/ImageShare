/**
 * Browse splat route — `GET /browse`, `GET /browse/photos`, `GET /browse/photos/2024`, …
 *
 * The splat (`$`) captures everything after `/browse/` as `_splat`. Each
 * segment of the splat is a path component of a `RelativePath` (relative,
 * forward-slash-delimited, never rooted). Segments arrive already
 * URL-decoded by the router, so they are joined with `/` to reconstruct the
 * `RelativePath` for the content-listing query.
 *
 * The `loader` kicks off the first-page fetch via `ensureInfiniteQueryData`
 * without awaiting it, so navigation commits immediately and the grid shows
 * its skeleton while the request is in flight (the `useFolderContent` hook
 * subscribes to the same in-flight query — TanStack Query dedupes). Cached
 * revisits resolve instantly and skip the skeleton.
 *
 * Folder navigation is wired to the router; opening an image shows the
 * fullscreen carousel (`ImageViewer`), controlled by `imagePath` state
 * (`null` = closed, a `RelativePath` = open at that image's index).
 */
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useState } from "react";
import ContentGrid from "@components/ContentGrid";
import ImageViewer from "@components/ImageViewer";
import { folderContentQueryOptions } from "@lib/api/contentQueries";

export const Route = createFileRoute("/browse/$")({
  loader: ({ context, params }) => {
    const segments = params._splat === undefined ? [] : params._splat.split("/");
    const relativePath = segments.join("/");
    const path = relativePath === "" ? undefined : relativePath;
    void context.queryClient.ensureInfiniteQueryData(folderContentQueryOptions(path));
  },
  component: BrowseComponent,
});

function BrowseComponent(): React.JSX.Element {
  const { _splat } = Route.useParams();
  const segments = _splat === undefined ? [] : _splat.split("/");
  const relativePath = segments.join("/");
  const path = relativePath === "" ? undefined : relativePath;
  const navigate = useNavigate();
  const [imagePath, setImagePath] = useState<string | undefined>();
  const handleNavigateFolder = useCallback(
    (folderPath: string) => navigate({ to: "/browse/$", params: { _splat: folderPath } }),
    [navigate],
  );
  const handleImageOpen = useCallback((openedPath: string) => setImagePath(openedPath), []);
  const handleClose = useCallback(() => setImagePath(undefined), []);
  return (
    <>
      <ContentGrid
        path={path}
        onNavigateFolder={handleNavigateFolder}
        onImageOpen={handleImageOpen}
      />
      {imagePath !== undefined && (
        <ImageViewer folderPath={path} imagePath={imagePath} onClose={handleClose} />
      )}
    </>
  );
}
