/**
 * ShareLinkResult — the share-link result dialog.
 *
 * Shown after the admin successfully generates a share token: presents the
 * JWT and (in later phases) the shareable URL, a QR code, and copy /
 * download actions. For now it displays the raw token so the generation
 * flow is wired end-to-end.
 */
import Dialog from "@components/ui/Dialog";

interface ShareLinkResultProps {
  /** The JWT string returned by the token-generation endpoint. */
  token: string;
  /** Whether the result dialog is open. */
  open: boolean;
  /** Open-state setter for the controlling trigger. */
  onOpenChange: (open: boolean) => void;
}

function ShareResultHeader(): React.JSX.Element {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Share link generated</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Copy this link or scan the QR code (coming soon) to share access.
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

export default function ShareLinkResult({ token, open, onOpenChange }: ShareLinkResultProps): React.JSX.Element {
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogContent>
        <ShareResultHeader />
        <code className="block break-all text-xs text-muted-foreground">{token}</code>
      </Dialog.DialogContent>
    </Dialog.Dialog>
  );
}
