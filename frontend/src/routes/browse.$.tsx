/**
 * Browse splat route — `GET /browse`, `GET /browse/photos`, `GET /browse/photos/2024`, …
 *
 * The splat (`$`) captures everything after `/browse/` as `_splat`. Each
 * segment of the splat is a path component of a `RelativePath` (relative,
 * forward-slash-delimited, never rooted). Segments arrive already
 * URL-decoded by the router, so they are joined with `/` to reconstruct the
 * `RelativePath` for the content-listing query.
 *
 * The `loader` prefetches the first page of folder content via
 * `ensureInfiniteQueryData` so the grid has data on first paint (critical
 * data that must block navigation). The `useFolderContent` hook in the
 * component reads the same cache.
 *
 * Folder navigation is wired to the router; image open is a no-op here
 * until the fullscreen carousel lands in Phase 6.
 */
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import ContentGrid from "@components/ContentGrid";
import { folderContentQueryOptions } from "@lib/api/contentQueries";
import { useCallback } from "react";

/** Phase 6 placeholder — opening an image will show the fullscreen carousel. */
function handleImageOpen() {}

export const Route = createFileRoute("/browse/$")({
  loader: async ({ context, params }) => {
    const segments = params._splat === undefined ? [] : params._splat.split("/");
    const relativePath = segments.join("/");
    const path = relativePath === "" ? undefined : relativePath;
    await context.queryClient.ensureInfiniteQueryData(folderContentQueryOptions(path));
  },
  component: BrowseComponent,
});

function BrowseComponent(): React.JSX.Element {
  const { _splat } = Route.useParams();
  const segments = _splat === undefined ? [] : _splat.split("/");
  const relativePath = segments.join("/");
  const path = relativePath === "" ? undefined : relativePath;
  const navigate = useNavigate();
  const handleNavigateFolder = useCallback(
    (folderPath: string) => navigate({ to: "/browse/$", params: { _splat: folderPath } }),
    [navigate],
  );
  return (
    <ContentGrid
      path={path}
      onNavigateFolder={handleNavigateFolder}
      onImageOpen={handleImageOpen}
    />
  );
}
