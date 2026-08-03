/**
 * Share-token mutation hook (`useShareMutation`) — TanStack Query mutation
 * for minting a share JWT.
 *
 * Hand-written wrapper around the hey-api-generated `generateToken` SDK
 * function (the generated infinite-query options are disabled, so — like
 * `contentQueries.ts` — we wrap the SDK call manually). Narrows the SDK
 * `data` to a string via `ensureString` (see `guards.ts`) rather than an
 * unchecked cast. Tracks the generated `token` and any `submitError`
 * (RFC 7807 message) so the host component can drive the form/result
 * dialog flow.
 *
 * Owns the `ShareFormValues` contract shared with `ShareFormDialog`.
 */
import { useCallback, useState } from "react";
import { ApiError } from "@lib/api/errors";
import { ensureString } from "@lib/api/guards";
import { generateToken } from "@lib/api/generated";
import { useMutation } from "@tanstack/react-query";

/** Values submitted by the share-link form. */
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
    mutationFn: async (values: ShareFormValues): Promise<string> => {
      const { data } = await generateToken({ query: values });
      return ensureString(data);
    },
    onSuccess: (generated) => {
      setToken(generated);
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
      mutation.mutate(values);
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
