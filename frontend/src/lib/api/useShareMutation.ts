/**
 * Share-token mutation hook (`useShareMutation`) — TanStack Query mutation
 * for minting a share JWT.
 *
 * The token endpoint is `POST /api/authentication/token/generate`, so hey-api's
 * `@tanstack/react-query` plugin generates `generateTokenMutation()` (see
 * `generated/@tanstack/react-query.gen.ts`); this hook spreads it and adds the
 * UI orchestration the generated options can't express: tracking the generated
 * `token` plus any `submitError` (RFC 7807 message) and the optional
 * `returnUrl` carried through from the form (it is not part of the token
 * request body — it is appended to the sign-in URL later).
 *
 * Owns the `ShareFormValues` contract shared with `ShareFormDialog`.
 */
import { useCallback, useState } from "react";
import { ApiError } from "@lib/api/errors";
import { generateTokenMutation } from "@lib/api/generated/@tanstack/react-query.gen";
import { toast } from "sonner";
import { useMutation } from "@tanstack/react-query";

/**
 * Values submitted by the share-link form. `name`, `filter`, and `endDate`
 * match the `GenerateTokenCommand` path parameters; `returnUrl` is an optional
 * site-relative path appended to the sign-in URL (not sent to the endpoint).
 */
export interface ShareFormValues {
  name: string;
  filter: string;
  endDate: string;
  returnUrl: string;
}

/** Extract a human-readable message from a mutation error (RFC 7807 ApiError). */
function extractErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return "Failed to generate share link.";
}

export interface ShareMutationState {
  token: string | undefined;
  returnUrl: string;
  submitError: string | undefined;
  isPending: boolean;
  handleGenerate: (values: ShareFormValues) => void;
  handleFormClose: () => void;
  handleResultClose: () => void;
}

/**
 * Drive the share-token generation flow.
 *
 * @param onSuccess - Called after the token is stored (e.g. to close the form).
 */
export function useShareMutation(onSuccess: () => void): ShareMutationState {
  const [token, setToken] = useState<string>();
  const [returnUrl, setReturnUrl] = useState("");
  const [submitError, setSubmitError] = useState<string>();
  const mutation = useMutation({
    ...generateTokenMutation(),
    onSuccess: (generated) => {
      setToken(generated);
      toast.success("Share link generated");
      onSuccess();
    },
    onError: (error) => {
      setSubmitError(extractErrorMessage(error));
    },
  });
  const handleGenerate = useCallback(
    (values: ShareFormValues) => {
      setSubmitError(undefined);
      setReturnUrl(values.returnUrl);
      mutation.mutate({
        path: { name: values.name, filter: values.filter, endDate: values.endDate },
      });
    },
    [mutation],
  );
  const handleFormClose = useCallback(() => {
    setSubmitError(undefined);
    setReturnUrl("");
    mutation.reset();
  }, [mutation]);
  const handleResultClose = useCallback(() => {
    setToken(undefined);
    setReturnUrl("");
    mutation.reset();
  }, [mutation]);
  return {
    token,
    returnUrl,
    submitError,
    isPending: mutation.isPending,
    handleGenerate,
    handleFormClose,
    handleResultClose,
  };
}
