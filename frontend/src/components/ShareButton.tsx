/**
 * ShareButton — admin share-link trigger.
 *
 * Renders the app-bar "Share" button that opens the `ShareLinkDialog`
 * form. On a valid submit it calls the orval-generated
 * `useGetApiAuthenticationTokenGenerate` mutation to mint a JWT. The form
 * stays open while the request is in flight; on success it closes the form
 * and opens `ShareLinkResult` to present the shareable URL + QR code. On a
 * 400/403 failure the RFC 7807 error message (from `ApiError`) is surfaced
 * inside the form dialog so the admin can correct and retry.
 *
 * Visibility is gated on `user.isAdmin` in the app bar (Phase 7 item 6).
 */
import { type getApiAuthenticationTokenGenerateResponse, useGetApiAuthenticationTokenGenerate } from "@lib/api/generated/authentication/authentication";
import { useCallback, useState } from "react";
import { ApiError } from "@lib/api/customFetcher";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import { Share } from "lucide-react";
import ShareLinkDialog from "@components/ShareLinkDialog";
import ShareLinkResult from "@components/ShareLinkResult";
import { tw } from "@lib/utils";

const ICON_CLASS = tw`size-4`;
const TRIGGER_CLASS = Button.buttonVariants({ variant: "ghost", size: "icon-sm" });

interface ShareFormValues {
  name: string;
  filter: string;
  endDate: string;
}

interface ShareButtonProps {
  /** Fired with the JWT once the token endpoint returns one. */
  onToken?: (token: string) => void;
}

/** Extract the JWT string from a successful token-generation response. */
function extractToken(response: getApiAuthenticationTokenGenerateResponse): string | undefined {
  return response.status === 200 ? response.data : undefined;
}

/** Extract a human-readable message from a mutation error (RFC 7807 ApiError). */
function extractErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return "Failed to generate share link.";
}

interface ShareMutationState {
  token: string | undefined;
  submitError: string | undefined;
  isPending: boolean;
  handleGenerate: (values: ShareFormValues) => void;
  handleFormClose: () => void;
  handleResultClose: () => void;
  onToken?: (token: string) => void;
}

function useShareMutation(
  onToken: ((token: string) => void) | undefined,
  onSuccess: () => void,
): ShareMutationState {
  const [token, setToken] = useState<string>();
  const [submitError, setSubmitError] = useState<string>();
  const mutation = useGetApiAuthenticationTokenGenerate({
    mutation: {
      onSuccess: (response) => {
        const generated = extractToken(response);
        if (generated !== undefined) {
          setToken(generated);
          onToken?.(generated);
          onSuccess();
        }
      },
      onError: (error) => {
        setSubmitError(extractErrorMessage(error));
      },
    },
  });
  const handleGenerate = useCallback(
    (values: ShareFormValues) => {
      setSubmitError(undefined);
      mutation.mutate({ params: values });
    },
    [mutation],
  );
  const handleFormClose = useCallback(() => {
    setSubmitError(undefined);
    mutation.reset();
  }, [mutation]);
  const handleResultClose = useCallback(() => {
    setToken(undefined);
    mutation.reset();
  }, [mutation]);
  return { token, submitError, isPending: mutation.isPending, handleGenerate, handleFormClose, handleResultClose, onToken };
}

interface ShareFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onGenerate: (values: ShareFormValues) => void;
  submitError?: string;
  isSubmitting: boolean;
}

function ShareFormDialog({ open, onOpenChange, onGenerate, submitError, isSubmitting }: ShareFormDialogProps): React.JSX.Element {
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogTrigger className={TRIGGER_CLASS} aria-label="Share">
        <Share className={ICON_CLASS} />
      </Dialog.DialogTrigger>
      <ShareLinkDialog onGenerate={onGenerate} submitError={submitError} isSubmitting={isSubmitting} />
    </Dialog.Dialog>
  );
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
