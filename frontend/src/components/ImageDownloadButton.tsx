/**
 * ImageDownloadButton — overlay download control for a single carousel image.
 *
 * A download icon that opens a dropdown listing the formats the backend
 * pre-converts (AVIF / WEBP / JPG). Selecting a format fetches the image
 * bytes from `GET /content/image/{path}` with the matching `Accept` header
 * (the single-image endpoint negotiates format via Accept, not a query
 * parameter) and triggers a blob download with the chosen file extension.
 *
 * The endpoint does not set `Content-Disposition: attachment`, so a plain
 * anchor would navigate to the image instead of downloading it — fetching
 * the bytes and creating an object URL is what forces the download.
 */
import { useCallback, useMemo } from "react";
import Button from "@components/ui/Button";
import DropdownMenu from "@components/ui/DropdownMenu";
import { Download } from "lucide-react";
import { imageUrl } from "@lib/api/urls";
import { type Translate, type TranslationKey, useTranslation } from "@lib/i18n";
import { toast } from "sonner";
import { tw } from "@lib/utils";

const ICON_CLASS = tw`size-4`;
const TRIGGER_CLASS = tw`absolute top-2 left-14 z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70`;
const TRIGGER_BUTTON = <Button variant="ghost" size="icon-sm" className={TRIGGER_CLASS} />;
const ITEM_ROW_CLASS = tw`grid grid-cols-[3rem_1fr] items-baseline gap-2`;
const ITEM_DESCRIPTION_CLASS = tw`text-xs text-muted-foreground`;

interface DownloadFormat {
  /** Backend `Accept` media type. */
  accept: string;
  /** File extension used for the downloaded file. */
  extension: string;
  /** I18n key under `download.formats` for the menu label. */
  labelKey: TranslationKey;
  /** I18n key under `download.formatDescriptions` for the menu hint. */
  descriptionKey: TranslationKey;
}

const DOWNLOAD_FORMATS = [
  {
    accept: "image/jpeg",
    extension: "jpg",
    labelKey: "download.formats.jpg",
    descriptionKey: "download.formatDescriptions.jpg",
  },
  {
    accept: "image/avif",
    extension: "avif",
    labelKey: "download.formats.avif",
    descriptionKey: "download.formatDescriptions.avif",
  },
  {
    accept: "image/webp",
    extension: "webp",
    labelKey: "download.formats.webp",
    descriptionKey: "download.formatDescriptions.webp",
  },
] as const satisfies readonly DownloadFormat[];

interface DownloadImageRequest {
  imagePath: string;
  fileName: string;
  format: DownloadFormat;
  translate: Translate;
}

function baseName(fileName: string): string {
  const dot = fileName.lastIndexOf(".");
  return dot > 0 ? fileName.slice(0, dot) : fileName;
}

async function downloadImage({
  imagePath,
  fileName,
  format,
  translate,
}: DownloadImageRequest): Promise<void> {
  try {
    const response = await fetch(imageUrl(imagePath, false), {
      credentials: "include",
      headers: { Accept: format.accept },
    });
    if (!response.ok) {
      throw new Error(`Unexpected response status ${response.status}`);
    }
    const blob = await response.blob();
    const objectUrl = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = objectUrl;
    anchor.download = `${baseName(fileName)}.${format.extension}`;
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
    toast.success(translate("imageViewer.downloadStarted"));
  } catch {
    toast.error(translate("imageViewer.downloadFailed"));
  }
}

interface ImageDownloadButtonProps {
  /** RelativePath of the image file currently shown in the carousel. */
  imagePath: string;
  /** File name (with extension) of the current image — used for the download filename. */
  imageName: string;
}

export default function ImageDownloadButton({
  imagePath,
  imageName,
}: ImageDownloadButtonProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  const formatsByExtension = useMemo(() => {
    const map = new Map<string, DownloadFormat>();
    for (const format of DOWNLOAD_FORMATS) {
      map.set(format.extension, format);
    }
    return map;
  }, []);
  const handleSelect = useCallback(
    (event: { currentTarget: { dataset: { formatExtension?: string } } }) => {
      const format = formatsByExtension.get(event.currentTarget.dataset.formatExtension ?? "");
      if (format === undefined) {
        return;
      }
      void downloadImage({ imagePath, fileName: imageName, format, translate });
    },
    [formatsByExtension, imagePath, imageName, translate],
  );
  return (
    <DropdownMenu.DropdownMenu>
      <DropdownMenu.DropdownMenuTrigger
        render={TRIGGER_BUTTON}
        aria-label={translate("imageViewer.download")}
      >
        <Download className={ICON_CLASS} />
      </DropdownMenu.DropdownMenuTrigger>
      <DropdownMenu.DropdownMenuContent align="start" sideOffset={4} className="w-fit min-w-72">
        {DOWNLOAD_FORMATS.map((format) => (
          <DropdownMenu.DropdownMenuItem
            key={format.extension}
            data-format-extension={format.extension}
            onClick={handleSelect}
          >
            <span className={ITEM_ROW_CLASS}>
              <span>{translate(format.labelKey)}</span>
              <span className={ITEM_DESCRIPTION_CLASS}>{translate(format.descriptionKey)}</span>
            </span>
          </DropdownMenu.DropdownMenuItem>
        ))}
      </DropdownMenu.DropdownMenuContent>
    </DropdownMenu.DropdownMenu>
  );
}
