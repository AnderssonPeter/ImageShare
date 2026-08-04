/**
 * Share-token mutation hook (`useShareMutation`) — TanStack Query mutation
 * for minting a share JWT.
 *
 * The token endpoint is `POST /api/authentication/token/generate`, so hey-api's
 * `@tanstack/react-query` plugin generates `generateTokenMutation()` (see
 * `generated/@tanstack/react-query.gen.ts`); this hook spreads it and adds the
 * UI orchestration the generated options can't express: tracking the generated
 * `token` plus any `submitError` (RFC 7807 message) and exposing form/result
 * close handlers so the host component can drive the dialog flow.
 *
 * Owns the `ShareFormValues` contract shared with `ShareFormDialog`.
 */
import { useCallback, useState } from "react";
import { ApiError } from "@lib/api/errors";
import { generateTokenMutation } from "@lib/api/generated/@tanstack/react-query.gen";
import { toast } from "sonner";
import { useMutation } from "@tanstack/react-query";

/** Values submitted by the share-link form (matches the `GenerateTokenCommand` body). */
export interface ShareFormValues {
  name: string;
  filter: string;
  endDate: string;
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
  submitError: string | undefined;
  isPending: boolean;
  handleGenerate: (values: ShareFormValues) => void;
  handleFormClose: () => void;
  handleResultClose: () => void;
  onToken?: (token: string) => void;
}

/**
 * Drive the share-token generation flow.
 *
 * @param onToken   - Fired with the JWT once the endpoint returns one.
 * @param onSuccess - Called after the token is stored (e.g. to close the form).
 */
export function useShareMutation(
  onToken: ((token: string) => void) | undefined,
  onSuccess: () => void,
): ShareMutationState {
  const [token, setToken] = useState<string>();
  const [submitError, setSubmitError] = useState<string>();
  const mutation = useMutation({
    ...generateTokenMutation(),
    onSuccess: (generated) => {
      setToken(generated);
      toast.success("Share link generated");
      onToken?.(generated);
      onSuccess();
    },
    onError: (error) => {
      setSubmitError(extractErrorMessage(error));
    },
  });
  const handleGenerate = useCallback(
    (values: ShareFormValues) => {
      setSubmitError(undefined);
      mutation.mutate({ path: values });
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
  return {
    token,
    submitError,
    isPending: mutation.isPending,
    handleGenerate,
    handleFormClose,
    handleResultClose,
    onToken,
  };
}
