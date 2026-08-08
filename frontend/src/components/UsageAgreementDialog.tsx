import { ApiError } from "@lib/api/errors";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { type UsageAgreementResponse } from "@lib/api/generated";
import { tw } from "@lib/utils";
import { useAcceptUsageAgreement } from "@lib/api/usageAgreement";
import { useCallback } from "react";
import { useTranslation, type Translate } from "@lib/i18n";

interface UsageAgreementDialogProps {
  /** The agreement to display (language + text + accepted). */
  agreement: UsageAgreementResponse;
}

const TEXT_CLASS = tw`max-h-[60vh] overflow-y-auto whitespace-pre-wrap rounded-md bg-muted/40 p-3 leading-relaxed`;

function acceptErrorMessage(error: unknown, translate: Translate): string {
  return error instanceof ApiError ? error.message : translate("usageAgreement.acceptError");
}

function UsageAgreementHeader(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{translate("usageAgreement.title")}</Dialog.DialogTitle>
      <Dialog.DialogDescription>{translate("usageAgreement.description")}</Dialog.DialogDescription>
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
  const { t: translate } = useTranslation();
  if (!(error instanceof Error)) {
    return;
  }
  return (
    <p role="alert" className="text-sm text-destructive">
      {acceptErrorMessage(error, translate)}
    </p>
  );
}

interface UsageAgreementFooterProps {
  onAccept: () => void;
  pending: boolean;
}

function UsageAgreementFooter({ onAccept, pending }: UsageAgreementFooterProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogFooter>
      <Button onClick={onAccept} disabled={pending}>
        {pending ? translate("usageAgreement.accepting") : translate("usageAgreement.accept")}
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
