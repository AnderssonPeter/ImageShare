/**
 * ShareFormDialog — the share trigger button and its hosting dialog.
 *
 * Renders the ghost icon button that opens the share-link form. Wraps the
 * `ShareLinkDialog` form inside a shadcn `Dialog` so the trigger and content
 * are wired together. The host (`ShareButton`) controls open-state and feeds
 * the form's `onGenerate`/`submitError`/`isSubmitting` props.
 */
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { Share } from "lucide-react";
import { type ShareFormValues } from "@lib/api/useShareMutation";
import ShareLinkDialog from "@components/ShareLinkDialog";
import { tw } from "@lib/utils";

const ICON_CLASS = tw`size-4`;
const TRIGGER_CLASS = Button.buttonVariants({ variant: "ghost", size: "icon-sm" });

interface ShareFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onGenerate: (values: ShareFormValues) => void;
  submitError?: string;
  isSubmitting: boolean;
}

export default function ShareFormDialog({
  open,
  onOpenChange,
  onGenerate,
  submitError,
  isSubmitting,
}: ShareFormDialogProps): React.JSX.Element {
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogTrigger className={TRIGGER_CLASS} aria-label="Share">
        <Share className={ICON_CLASS} />
      </Dialog.DialogTrigger>
      <ShareLinkDialog
        onGenerate={onGenerate}
        submitError={submitError}
        isSubmitting={isSubmitting}
      />
    </Dialog.Dialog>
  );
}
