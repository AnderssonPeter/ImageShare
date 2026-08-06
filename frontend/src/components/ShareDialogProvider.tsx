/**
 * ShareDialogProvider — single source of truth for opening the admin share
 * dialog from anywhere in the app.
 *
 * Hosts the share-link form dialog and the generated-link result dialog
 * (plus the token-generation mutation). Consumers call `openShare(initialReturnUrl?)`
 * via the `useShareDialog` hook (`@lib/shareDialogContext`) — the app-bar
 * trigger opens it empty, the image viewer opens it prefilled with the
 * current image permalink. Only rendered for admin users (`enabled` prop) so
 * the mutation/dialog wiring is inert for non-admins.
 */
import { type ReactNode, useCallback, useMemo, useState } from "react";
import { ShareDialogContext, type ShareDialogValue } from "@lib/shareDialogContext";
import ShareFormDialog from "@components/ShareFormDialog";
import ShareLinkResult from "@components/ShareLinkResult";
import { useShareMutation } from "@lib/api/useShareMutation";

interface ShareDialogsProps {
  enabled: boolean;
  formOpen: boolean;
  formSession: number;
  initialReturnUrl: string | undefined;
  onFormOpenChange: (open: boolean) => void;
  onGenerate: ReturnType<typeof useShareMutation>["handleGenerate"];
  submitError: string | undefined;
  isPending: boolean;
  token: string | undefined;
  returnUrl: string;
  onResultClose: () => void;
}

function ShareDialogs({
  enabled,
  formOpen,
  formSession,
  initialReturnUrl,
  onFormOpenChange,
  onGenerate,
  submitError,
  isPending,
  token,
  returnUrl,
  onResultClose,
}: ShareDialogsProps): React.JSX.Element | undefined {
  if (!enabled) {
    return;
  }
  return (
    <>
      <ShareFormDialog
        key={formSession}
        open={formOpen}
        onOpenChange={onFormOpenChange}
        onGenerate={onGenerate}
        submitError={submitError}
        isSubmitting={isPending}
        initialReturnUrl={initialReturnUrl}
      />
      {token !== undefined && (
        <ShareLinkResult token={token} returnUrl={returnUrl} open onOpenChange={onResultClose} />
      )}
    </>
  );
}

interface ShareDialogState {
  token: string | undefined;
  returnUrl: string;
  submitError: string | undefined;
  isPending: boolean;
  handleGenerate: ReturnType<typeof useShareMutation>["handleGenerate"];
  formOpen: boolean;
  formSession: number;
  initialReturnUrl: string | undefined;
  openShare: (prefillReturnUrl?: string) => void;
  onFormOpenChange: (open: boolean) => void;
  onResultClose: () => void;
}

/** Owns the mutation, form-open state, and the `openShare` consumer API. */
function useShareDialogState(): ShareDialogState {
  const [formOpen, setFormOpen] = useState(false);
  const [formSession, setFormSession] = useState(0);
  const [initialReturnUrl, setInitialReturnUrl] = useState<string | undefined>();
  const {
    token,
    returnUrl,
    submitError,
    isPending,
    handleGenerate,
    handleFormClose,
    handleResultClose,
  } = useShareMutation(() => setFormOpen(false));
  const openShare = useCallback((prefillReturnUrl?: string) => {
    // A directly-wired onClick forwards a MouseEvent; ignore non-string args so it never leaks as "[object Object]".
    setInitialReturnUrl(typeof prefillReturnUrl === "string" ? prefillReturnUrl : undefined);
    setFormSession((session) => session + 1);
    setFormOpen(true);
  }, []);
  const onFormOpenChange = useCallback(
    (open: boolean) => {
      setFormOpen(open);
      if (!open) {
        handleFormClose();
        setInitialReturnUrl(undefined);
      }
    },
    [handleFormClose],
  );
  const onResultClose = useCallback(() => {
    handleResultClose();
    setInitialReturnUrl(undefined);
  }, [handleResultClose]);
  return {
    token,
    returnUrl,
    submitError,
    isPending,
    handleGenerate,
    formOpen,
    formSession,
    initialReturnUrl,
    openShare,
    onFormOpenChange,
    onResultClose,
  };
}

interface ShareDialogProviderProps {
  children: ReactNode;
  /** Whether the share feature is available (gated on `user.isAdmin`). */
  enabled: boolean;
}

export default function ShareDialogProvider({
  children,
  enabled,
}: ShareDialogProviderProps): React.JSX.Element {
  const {
    token,
    returnUrl,
    submitError,
    isPending,
    handleGenerate,
    formOpen,
    formSession,
    initialReturnUrl,
    openShare,
    onFormOpenChange,
    onResultClose,
  } = useShareDialogState();
  const value = useMemo<ShareDialogValue>(() => ({ openShare }), [openShare]);
  return (
    <ShareDialogContext.Provider value={value}>
      {children}
      <ShareDialogs
        enabled={enabled}
        formOpen={formOpen}
        formSession={formSession}
        initialReturnUrl={initialReturnUrl}
        onFormOpenChange={onFormOpenChange}
        onGenerate={handleGenerate}
        submitError={submitError}
        isPending={isPending}
        token={token}
        returnUrl={returnUrl}
        onResultClose={onResultClose}
      />
    </ShareDialogContext.Provider>
  );
}
