/**
 * ShareButton — admin share-link trigger.
 *
 * Orchestrates the share-link flow: the trigger button + form dialog
 * (`ShareFormDialog`) and the success result dialog (`ShareLinkResult`).
 * Token generation is handled by the `useShareMutation` hook; this component
 * only wires open-state and renders the two dialogs.
 *
 * Visibility is gated on `user.isAdmin` in the app bar (Phase 7 item 6).
 */
import { useCallback, useState } from "react";
import ShareFormDialog from "@components/ShareFormDialog";
import ShareLinkResult from "@components/ShareLinkResult";
import { useShareMutation } from "@lib/api/useShareMutation";

interface ShareButtonProps {
  /** Fired with the JWT once the token endpoint returns one. */
  onToken?: (token: string) => void;
}

export default function ShareButton({ onToken }: ShareButtonProps): React.JSX.Element {
  const [formOpen, setFormOpen] = useState(false);
  const { token, submitError, isPending, handleGenerate, handleFormClose, handleResultClose } =
    useShareMutation(onToken, () => setFormOpen(false));
  const handleFormOpenChange = useCallback(
    (open: boolean) => {
      setFormOpen(open);
      if (!open) {
        handleFormClose();
      }
    },
    [handleFormClose],
  );
  return (
    <>
      <ShareFormDialog
        open={formOpen}
        onOpenChange={handleFormOpenChange}
        onGenerate={handleGenerate}
        submitError={submitError}
        isSubmitting={isPending}
      />
      {token !== undefined && (
        <ShareLinkResult token={token} open onOpenChange={handleResultClose} />
      )}
    </>
  );
}
