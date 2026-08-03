import { Check, Copy } from "lucide-react";
import { type RefObject, useCallback, useRef, useState } from "react";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { QRCodeSVG } from "qrcode.react";
import { buildShareUrl } from "@lib/api/urls";
import logoUrl from "@assets/logo.svg?url";

interface ShareLinkResultProps {
  /** The JWT string returned by the token-generation endpoint. */
  token: string;
  /** Whether the result dialog is open. */
  open: boolean;
  /** Open-state setter for the controlling trigger. */
  onOpenChange: (open: boolean) => void;
}

const QR_LOGO_SETTINGS = { src: logoUrl, height: 40, width: 40, excavate: true } as const;
const QR_SIZE = 200;
const COPIED_FEEDBACK_MS = 2000;

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
}

function useShareActions(url: string) {
  const [copied, setCopied] = useState(false);
  const handleCopy = useCallback(async () => {
    await navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), COPIED_FEEDBACK_MS);
  }, [url]);
  return { copied, handleCopy };
}

function CopyLinkButton({ copied, onClick }: { copied: boolean; onClick: () => void }): React.JSX.Element {
  return (
    <Button type="button" variant="outline" onClick={onClick}>
      {copied ? <Check className="size-4" /> : <Copy className="size-4" />}
      {copied ? "Copied" : "Copy link"}
    </Button>
  );
}

function ShareActions({ url }: ShareActionsProps): React.JSX.Element {
  const { copied, handleCopy } = useShareActions(url);
  return (
    <Dialog.DialogFooter>
      <CopyLinkButton copied={copied} onClick={handleCopy} />
    </Dialog.DialogFooter>
  );
}

export default function ShareLinkResult({ token, open, onOpenChange }: ShareLinkResultProps): React.JSX.Element {
  const svgRef = useRef<SVGSVGElement>(null);
  const shareUrl = buildShareUrl(token, globalThis.location.origin);
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogContent>
        <ShareResultHeader />
        <ShareQrCode url={shareUrl} svgRef={svgRef} />
        <ShareActions url={shareUrl} />
      </Dialog.DialogContent>
    </Dialog.Dialog>
  );
}
