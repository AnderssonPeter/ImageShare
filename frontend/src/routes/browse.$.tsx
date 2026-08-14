import { createFileRoute, useLocation } from "@tanstack/react-router";
import ContentGrid from "@components/ContentGrid";
import ImageViewer from "@components/ImageViewer";
import { folderContentQueryOptions } from "@lib/api/contentQueries";
import { useBrowseNavigation } from "@lib/browseNavigation";
import { useCallback } from "react";
import { useShareDialog } from "@lib/shareDialogContext";
import { useUser } from "@lib/userContext";

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
    void context.queryClient.ensureQueryData(folderContentQueryOptions(path));
  },
  component: BrowseComponent,
});

function BrowseComponent(): React.JSX.Element {
  const { _splat } = Route.useParams();
  const { image } = Route.useSearch();
  const { data: user } = useUser();
  const path = splatToRelativePath(_splat) || undefined;
  const { navigateFolder, openImage, changeImage, closeImage } = useBrowseNavigation(_splat);
  const { openShare } = useShareDialog();
  const location = useLocation();
  const handleShareImage = useCallback(() => openShare(location.href), [openShare, location.href]);
  const onShare = user?.isAdmin === true ? handleShareImage : undefined;
  return (
    <>
      <ContentGrid path={path} onNavigateFolder={navigateFolder} onImageOpen={openImage} />
      {image !== undefined && (
        <ImageViewer
          folderPath={path}
          imageName={image}
          onImageChange={changeImage}
          onClose={closeImage}
          onShare={onShare}
        />
      )}
    </>
  );
}
