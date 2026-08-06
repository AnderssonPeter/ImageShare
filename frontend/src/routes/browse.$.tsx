import ContentGrid from "@components/ContentGrid";
import ImageViewer from "@components/ImageViewer";
import { createFileRoute } from "@tanstack/react-router";
import { folderContentQueryOptions } from "@lib/api/contentQueries";
import { useBrowseNavigation } from "@lib/browseNavigation";

/** Search params for the browse route — `image` is the open image's filename (permalink). */
interface BrowseSearch {
  image?: string;
}

/** Reconstruct a RelativePath (relative, forward-slash-delimited) from the raw splat. */
function splatToRelativePath(splat: string | undefined): string {
  const segments = splat === undefined ? [] : splat.split("/");
  return segments.join("/");
}

export const Route = createFileRoute("/browse/$")({
  validateSearch: (search: Record<string, unknown>): BrowseSearch => ({
    image: typeof search.image === "string" && search.image.length > 0 ? search.image : undefined,
  }),
  loader: ({ context, params }) => {
    const path = splatToRelativePath(params._splat) || undefined;
    void context.queryClient.ensureInfiniteQueryData(folderContentQueryOptions(path));
  },
  component: BrowseComponent,
});

function BrowseComponent(): React.JSX.Element {
  const { _splat } = Route.useParams();
  const { image } = Route.useSearch();
  const path = splatToRelativePath(_splat) || undefined;
  const { navigateFolder, openImage, changeImage, closeImage } = useBrowseNavigation(_splat);
  return (
    <>
      <ContentGrid path={path} onNavigateFolder={navigateFolder} onImageOpen={openImage} />
      {image !== undefined && (
        <ImageViewer
          folderPath={path}
          imageName={image}
          onImageChange={changeImage}
          onClose={closeImage}
        />
      )}
    </>
  );
}
