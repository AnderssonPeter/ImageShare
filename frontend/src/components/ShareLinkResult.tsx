import { Check, Copy, Mail } from "lucide-react";
import { type RefObject, useCallback, useEffect, useRef, useState } from "react";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { QRCodeSVG } from "qrcode.react";
import { buildShareUrl } from "@lib/api/urls";
import logoSource from "@assets/logo.svg?raw";
import { type Translate, useTranslation } from "@lib/i18n";
import { svgElementToPngFile } from "@lib/svgToPng";
import { toast } from "sonner";

interface ShareLinkResultProps {
  /** The JWT string returned by the token-generation endpoint. */
  token: string;
  /** Optional site-relative path appended as `?returnUrl=` to the sign-in URL. */
  returnUrl?: string;
  /** Whether the result dialog is open. */
  open: boolean;
  /** Open-state setter for the controlling trigger. */
  onOpenChange: (open: boolean) => void;
}

// The logo is embedded as a data URI so the QR <svg> is self-contained:
// `svgElementToPngFile` serializes the SVG to a blob and decodes it, which
// Cannot resolve external sub-resource URLs (e.g. the hashed
// `/assets/logo-*.svg`) from within the blob origin.
const LOGO_DATA_URL = `data:image/svg+xml,${encodeURIComponent(logoSource)}`;
const QR_LOGO_SETTINGS = { src: LOGO_DATA_URL, height: 40, width: 40, excavate: true } as const;
const QR_SIZE = 200;
const COPIED_FEEDBACK_MS = 2000;
const QR_FILENAME = "share-qr.png";

function buildMailtoUrl(url: string, translate: Translate): string {
  const params = new URLSearchParams({
    subject: translate("share.shareTitle"),
    body: url,
  });
  return `mailto:?${params.toString()}`;
}

async function dispatchQrFile(file: File, url: string, translate: Translate): Promise<void> {
  if (navigator.canShare?.({ files: [file] }) === true) {
    await navigator.share({ files: [file], title: translate("share.shareTitle"), text: url, url });
    toast.success(translate("share.toasts.shared"));
    return;
  }
  globalThis.location.href = buildMailtoUrl(url, translate);
}

function handleEmailError(error: unknown, translate: Translate): void {
  console.error("Error sending email:", error);
  if (error instanceof DOMException && error.name === "AbortError") {
    return;
  }
  toast.error(translate("share.toasts.emailError"));
}

function ShareResultHeader(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{translate("share.resultTitle")}</Dialog.DialogTitle>
      <Dialog.DialogDescription>{translate("share.resultDescription")}</Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

interface ShareQrCodeProps {
  url: string;
  svgRef: RefObject<SVGSVGElement | null>;
}

function ShareQrCode({ url, svgRef }: ShareQrCodeProps): React.JSX.Element {
  return (
    <div className="flex justify-center">
      <QRCodeSVG
        ref={svgRef}
        value={url}
        size={QR_SIZE}
        level="H"
        marginSize={1}
        imageSettings={QR_LOGO_SETTINGS}
      />
    </div>
  );
}

interface ShareActionsProps {
  url: string;
  svgRef: RefObject<SVGSVGElement | null>;
}

function useQrFile(svgRef: RefObject<SVGSVGElement | null>, url: string) {
  const [qrFile, setQrFile] = useState<File | undefined>();
  // Pre-rasterize the QR <svg> to a PNG File once it is mounted (and whenever
  // The share URL changes). The Web Share API requires `navigator.share()` to
  // Be called within a user gesture; rasterizing eagerly means the click
  // Handler can invoke `share()` synchronously without awaiting an image
  // Decode that would expire the transient user activation.
  useEffect(() => {
    let cancelled = false;
    const svg = svgRef.current;
    if (svg === null) {
      return;
    }
    void (async () => {
      try {
        const file = await svgElementToPngFile(svg, QR_SIZE, QR_FILENAME);
        if (!cancelled) {
          setQrFile(file);
        }
      } catch {
        // Rasterization failed — the share button stays disabled.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [svgRef, url]);
  return qrFile;
}

function useShareActions(url: string, svgRef: RefObject<SVGSVGElement | null>) {
  const { t: translate } = useTranslation();
  const [copied, setCopied] = useState(false);
  const [sending, setSending] = useState(false);
  const qrFile = useQrFile(svgRef, url);
  const handleCopy = useCallback(async () => {
    await navigator.clipboard.writeText(url);
    setCopied(true);
    toast.success(translate("share.toasts.copied"));
    setTimeout(() => setCopied(false), COPIED_FEEDBACK_MS);
  }, [url, translate]);
  const handleSendEmail = useCallback(async () => {
    if (qrFile === undefined) {
      return;
    }
    setSending(true);
    try {
      await dispatchQrFile(qrFile, url, translate);
    } catch (error) {
      handleEmailError(error, translate);
    } finally {
      setSending(false);
    }
  }, [qrFile, url, translate]);
  return { copied, sending, qrFileReady: qrFile !== undefined, handleCopy, handleSendEmail };
}

function CopyLinkButton({
  copied,
  onClick,
}: {
  copied: boolean;
  onClick: () => void;
}): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Button type="button" variant="outline" onClick={onClick}>
      {copied ? <Check className="size-4" /> : <Copy className="size-4" />}
      {copied ? translate("share.copied") : translate("share.copyLink")}
    </Button>
  );
}

function SendEmailButton({
  sending,
  disabled,
  onClick,
}: {
  sending: boolean;
  disabled: boolean;
  onClick: () => void;
}): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Button type="button" variant="outline" disabled={sending || disabled} onClick={onClick}>
      <Mail className="size-4" />
      {sending ? translate("share.sending") : translate("share.share")}
    </Button>
  );
}

function ShareActions({ url, svgRef }: ShareActionsProps): React.JSX.Element {
  const { copied, sending, qrFileReady, handleCopy, handleSendEmail } = useShareActions(
    url,
    svgRef,
  );
  return (
    <Dialog.DialogFooter>
      <CopyLinkButton copied={copied} onClick={handleCopy} />
      <SendEmailButton sending={sending} disabled={!qrFileReady} onClick={handleSendEmail} />
    </Dialog.DialogFooter>
  );
}

export default function ShareLinkResult({
  token,
  returnUrl,
  open,
  onOpenChange,
}: ShareLinkResultProps): React.JSX.Element {
  const svgRef = useRef<SVGSVGElement>(null);
  const shareUrl = buildShareUrl(token, globalThis.location.origin, returnUrl);
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogContent>
        <ShareResultHeader />
        <ShareQrCode url={shareUrl} svgRef={svgRef} />
        <ShareActions url={shareUrl} svgRef={svgRef} />
      </Dialog.DialogContent>
    </Dialog.Dialog>
  );
}
