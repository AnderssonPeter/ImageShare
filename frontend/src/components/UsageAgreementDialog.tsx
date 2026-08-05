/**
 * UsageAgreementDialog — blocking modal presenting the usage agreement.
 *
 * Rendered by `UsageAgreementGate` when the agreement is enabled and not yet
 * accepted (`accepted === false`). It is non-dismissable: there is no close
 * button, outside-press is disabled, and `onOpenChange` ignores Esc/escape so
 * the only way out is to accept. Accepting calls `POST /api/usage-agreement/accept`;
 * on success the gate invalidates the agreement query, refetches with
 * `accepted: true`, and unmounts this dialog. The agreement text is shown
 * verbatim with preserved line breaks and a scroll region so long texts fit.
 */
import { ApiError } from "@lib/api/errors";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { type UsageAgreementResponse } from "@lib/api/generated";
import { tw } from "@lib/utils";
import { useAcceptUsageAgreement } from "@lib/api/usageAgreement";
import { useCallback } from "react";

interface UsageAgreementDialogProps {
  /** The agreement to display (language + text + accepted). */
  agreement: UsageAgreementResponse;
}

const TEXT_CLASS = tw`max-h-[60vh] overflow-y-auto whitespace-pre-wrap rounded-md bg-muted/40 p-3 leading-relaxed`;

function acceptErrorMessage(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : "Failed to accept the agreement. Please try again.";
}

function UsageAgreementHeader(): React.JSX.Element {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Usage agreement</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Please read and accept the agreement below to continue. You may be asked again if it changes.
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

function UsageAgreementText({ text }: { text: string }): React.JSX.Element {
  return (
    <div className={TEXT_CLASS} data-testid="usage-agreement-text">
      {text}
    </div>
  );
}

function UsageAgreementError({ error }: { error: unknown }): React.JSX.Element | undefined {
  if (!(error instanceof Error)) {
    return;
  }
  return (
    <p role="alert" className="text-sm text-destructive">
      {acceptErrorMessage(error)}
    </p>
  );
}

interface UsageAgreementFooterProps {
  onAccept: () => void;
  pending: boolean;
}

function UsageAgreementFooter({ onAccept, pending }: UsageAgreementFooterProps): React.JSX.Element {
  return (
    <Dialog.DialogFooter>
      <Button onClick={onAccept} disabled={pending}>
        {pending ? "Accepting…" : "Accept"}
      </Button>
    </Dialog.DialogFooter>
  );
}

export default function UsageAgreementDialog({
  agreement,
}: UsageAgreementDialogProps): React.JSX.Element {
  const accept = useAcceptUsageAgreement();
  const handleAccept = useCallback(() => {
    accept.mutate({});
  }, [accept]);
  // Controlled + non-dismissable: the gate unmounts this dialog only once the
  // Backend reports `accepted: true`, so all close requests (Esc) are ignored.
  const handleOpenChange = useCallback(() => {}, []);

  return (
    <Dialog.Dialog open onOpenChange={handleOpenChange} disablePointerDismissal>
      <Dialog.DialogContent showCloseButton={false} className="sm:max-w-lg">
        <UsageAgreementHeader />
        <UsageAgreementText text={agreement.text} />
        <UsageAgreementError error={accept.error} />
        <UsageAgreementFooter onAccept={handleAccept} pending={accept.isPending} />
      </Dialog.DialogContent>
    </Dialog.Dialog>
  );
}
