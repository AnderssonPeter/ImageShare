/**
 * DownloadButton — app-bar download icon that opens a `DownloadDialog`.
 *
 * A download icon that, when pressed, opens a dialog asking whether the
 * download should be recursive and which image format to include. The
 * download is triggered by navigating to `downloadUrl(folder, formats)`
 * — the backend responds with a zip stream (`Content-Disposition:
 * attachment`), so the browser downloads without leaving the page.
 *
 * Form state (recursive, format) is managed by TanStack Form inside
 * `DownloadDialog`; this component only controls dialog open/close and
 * starts the download on submit.
 */
import { useCallback, useState } from "react";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { Download } from "lucide-react";
import DownloadDialog from "@components/DownloadDialog";
import { downloadUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";

const ICON_CLASS = tw`size-4`;
const TRIGGER_CLASS = Button.buttonVariants({ variant: "ghost", size: "icon-sm" });

interface DownloadButtonProps {
  /** Current folder RelativePath (undefined or empty = root listing). */
  path?: string;
}

function startDownload(folder: string, formats: readonly string[]): void {
  globalThis.location.href = downloadUrl(folder, formats);
}

export default function DownloadButton({ path }: DownloadButtonProps): React.JSX.Element {
  const folder = path ?? "";
  const [open, setOpen] = useState(false);
  const handleDownload = useCallback(
    (values: { recursive: boolean; format: string }) => {
      setOpen(false);
      startDownload(folder, values.format === "" ? [] : [values.format]);
    },
    [folder],
  );
  return (
    <Dialog.Dialog open={open} onOpenChange={setOpen}>
      <Dialog.DialogTrigger className={TRIGGER_CLASS} aria-label="Download folder">
        <Download className={ICON_CLASS} />
      </Dialog.DialogTrigger>
      <DownloadDialog onDownload={handleDownload} />
    </Dialog.Dialog>
  );
}
