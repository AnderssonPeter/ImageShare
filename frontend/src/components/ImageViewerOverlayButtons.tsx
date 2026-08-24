import Button from "@components/ui/Button";
import ShareButton from "@components/ShareButton";
import ImageDownloadButton from "@components/ImageDownloadButton";
import { X } from "lucide-react";
import { tw } from "@lib/utils";
import { useTranslation } from "@lib/i18n";

const CLOSE_CLASS = tw`absolute top-2 right-2 z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70`;
const ICON_CLASS = tw`size-4`;

interface ImageViewerOverlayButtonsProps {
  onClose: () => void;
  onShare: (() => void) | undefined;
  downloadImagePath: string | undefined;
  downloadImageName: string | undefined;
}

export default function ImageViewerOverlayButtons({
  onClose,
  onShare,
  downloadImagePath,
  downloadImageName,
}: ImageViewerOverlayButtonsProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <>
      <ShareButton onClick={onShare} variant="overlay" ariaLabel={translate("imageViewer.share")} />
      {downloadImagePath !== undefined && downloadImageName !== undefined && (
        <ImageDownloadButton imagePath={downloadImagePath} imageName={downloadImageName} />
      )}
      <Button
        variant="ghost"
        size="icon-sm"
        className={CLOSE_CLASS}
        onClick={onClose}
        aria-label={translate("imageViewer.close")}
      >
        <X className={ICON_CLASS} />
      </Button>
    </>
  );
}
