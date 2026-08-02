/**
 * ShareButton — admin share-link trigger.
 *
 * Renders the app-bar "Share" button that opens the `ShareLinkDialog`
 * form. On a valid submit it calls the orval-generated
 * `useGetApiAuthenticationTokenGenerate` mutation to mint a JWT, then
 * closes the form and opens `ShareLinkResult` to present the token
 * (and, in later phases, the shareable URL + QR code + copy/download).
 *
 * Visibility is gated on `user.isAdmin` in the app bar (Phase 7 item 6).
 */
import { type getApiAuthenticationTokenGenerateResponse, useGetApiAuthenticationTokenGenerate } from "@lib/api/generated/authentication/authentication";
import { useCallback, useState } from "react";
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

interface ShareFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onGenerate: (values: ShareFormValues) => void;
}

function ShareFormDialog({ open, onOpenChange, onGenerate }: ShareFormDialogProps): React.JSX.Element {
  return (
    <Dialog.Dialog open={open} onOpenChange={onOpenChange}>
      <Dialog.DialogTrigger className={TRIGGER_CLASS} aria-label="Share">
        <Share className={ICON_CLASS} />
      </Dialog.DialogTrigger>
      <ShareLinkDialog onGenerate={onGenerate} />
    </Dialog.Dialog>
  );
}

export default function ShareButton({ onToken }: ShareButtonProps): React.JSX.Element {
  const [formOpen, setFormOpen] = useState(false);
  const [token, setToken] = useState<string>();
  const mutation = useGetApiAuthenticationTokenGenerate({
    mutation: {
      onSuccess: (response) => {
        const generated = extractToken(response);
        if (generated !== undefined) {
          setToken(generated);
          onToken?.(generated);
        }
      },
    },
  });
  const handleGenerate = useCallback(
    (values: ShareFormValues) => {
      mutation.mutate({ params: values });
      setFormOpen(false);
    },
    [mutation],
  );
  const handleResultOpenChange = useCallback(
    (open: boolean) => {
      if (!open) {
        setToken(undefined);
        mutation.reset();
      }
    },
    [mutation],
  );
  return (
    <>
      <ShareFormDialog open={formOpen} onOpenChange={setFormOpen} onGenerate={handleGenerate} />
      {token !== undefined && (
        <ShareLinkResult token={token} open onOpenChange={handleResultOpenChange} />
      )}
    </>
  );
}
