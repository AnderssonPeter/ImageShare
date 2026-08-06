import { Check, Copy, Mail } from "lucide-react";
import { type RefObject, useCallback, useRef, useState } from "react";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { QRCodeSVG } from "qrcode.react";
import { buildShareUrl } from "@lib/api/urls";
import logoUrl from "@assets/logo.svg?url";
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

const QR_LOGO_SETTINGS = { src: logoUrl, height: 40, width: 40, excavate: true } as const;
const QR_SIZE = 200;
const COPIED_FEEDBACK_MS = 2000;
const QR_FILENAME = "share-qr.png";
const SHARE_TITLE = "ImageShare link";
const MAILTO_SUBJECT = "ImageShare link";

function buildMailtoUrl(url: string): string {
  const params = new URLSearchParams({
    subject: MAILTO_SUBJECT,
    body: url,
  });
  return `mailto:?${params.toString()}`;
}

async function dispatchQrFile(file: File, url: string): Promise<void> {
  if (navigator.canShare?.({ files: [file] }) === true) {
    await navigator.share({ files: [file], title: SHARE_TITLE, text: url });
    toast.success("Shared");
    return;
  }
  globalThis.location.href = buildMailtoUrl(url);
}

function handleEmailError(error: unknown): void {
  if (error instanceof DOMException && error.name === "AbortError") {
    return;
  }
  toast.error("Could not send email");
}

function ShareResultHeader(): React.JSX.Element {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Share link generated</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Share this link or scan the QR code to grant access to the matching folders.
      </Dialog.DialogDescription>
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

function useShareActions(url: string, svgRef: RefObject<SVGSVGElement | null>) {
  const [copied, setCopied] = useState(false);
  const [sending, setSending] = useState(false);
  const handleCopy = useCallback(async () => {
    await navigator.clipboard.writeText(url);
    setCopied(true);
    toast.success("Link copied");
    setTimeout(() => setCopied(false), COPIED_FEEDBACK_MS);
  }, [url]);
  const handleSendEmail = useCallback(async () => {
    const svg = svgRef.current;
    if (svg === null) {
      return;
    }
    setSending(true);
    try {
      const file = await svgElementToPngFile(svg, QR_SIZE, QR_FILENAME);
      await dispatchQrFile(file, url);
    } catch (error) {
      handleEmailError(error);
    } finally {
      setSending(false);
    }
  }, [svgRef, url]);
  return { copied, sending, handleCopy, handleSendEmail };
}

function CopyLinkButton({
  copied,
  onClick,
}: {
  copied: boolean;
  onClick: () => void;
}): React.JSX.Element {
  return (
    <Button type="button" variant="outline" onClick={onClick}>
      {copied ? <Check className="size-4" /> : <Copy className="size-4" />}
      {copied ? "Copied" : "Copy link"}
    </Button>
  );
}

function SendEmailButton({
  sending,
  onClick,
}: {
  sending: boolean;
  onClick: () => void;
}): React.JSX.Element {
  return (
    <Button type="button" variant="outline" disabled={sending} onClick={onClick}>
      <Mail className="size-4" />
      {sending ? "Sending…" : "Send to email"}
    </Button>
  );
}

function ShareActions({ url, svgRef }: ShareActionsProps): React.JSX.Element {
  const { copied, sending, handleCopy, handleSendEmail } = useShareActions(url, svgRef);
  return (
    <Dialog.DialogFooter>
      <CopyLinkButton copied={copied} onClick={handleCopy} />
      <SendEmailButton sending={sending} onClick={handleSendEmail} />
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
