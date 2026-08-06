import Dialog from "@components/ui/Dialog";
import { type ShareFormValues } from "@lib/api/useShareMutation";
import ShareLinkDialog from "@components/ShareLinkDialog";

interface ShareFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onGenerate: (values: ShareFormValues) => void;
  submitError?: string;
  isSubmitting: boolean;
  initialReturnUrl?: string;
}

export default function ShareFormDialog({
  open,
  onOpenChange,
  onGenerate,
  submitError,
  isSubmitting,
  initialReturnUrl,
}: ShareFormDialogProps): React.JSX.Element {
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <ShareLinkDialog
        onGenerate={onGenerate}
        submitError={submitError}
        isSubmitting={isSubmitting}
        initialReturnUrl={initialReturnUrl}
      />
    </Dialog.Dialog>
  );
}
