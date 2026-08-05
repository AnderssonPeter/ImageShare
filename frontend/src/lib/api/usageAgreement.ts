/**
 * Usage-agreement query + accept mutation.
 *
 * The usage agreement is an optional server-side feature. When the
 * `UsageAgreement` config is empty the backend disables the endpoints and
 * `GET /api/usage-agreement` returns 404 — the frontend treats that as
 * "feature off, browse normally" (see `isUsageAgreementDisabled`). When the
 * feature is enabled the GET returns the agreement matching the request's
 * `Accept-Language` (sent automatically by the browser) plus an `accepted`
 * flag. `accepted === false` makes `UsageAgreementGate` render a blocking
 * dialog before downloads and full-image serving work — those endpoints stay
 * 403-gated on the backend until the cookie is set.
 *
 * The acceptance cookie is HttpOnly, so the client cannot read it; it relies
 * on the GET response's `accepted` field instead. Accepting is
 * `POST /api/usage-agreement/accept`; on success the agreement query is
 * invalidated so it refetches with `accepted: true` and the gate unmounts
 * the dialog. If the configured text changes server-side, the stored hash no
 * longer matches and `accepted` becomes `false` again, prompting re-accept.
 */
import {
  acceptUsageAgreementMutation,
  getUsageAgreementOptions,
  getUsageAgreementQueryKey,
} from "@lib/api/generated/@tanstack/react-query.gen";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@lib/api/errors";

/**
 * Query options for `GET /api/usage-agreement`. A 404 ("feature disabled") is
 * a valid terminal state, not a transient failure, so it is excluded from
 * retries; other errors retry up to three times (the React Query default).
 */
export function usageAgreementQueryOptions() {
  return {
    ...getUsageAgreementOptions(),
    retry: (failureCount: number, error: unknown) =>
      !(error instanceof ApiError && error.status === 404) && failureCount < 3,
  };
}

/** Whether a usage-agreement query error means the feature is disabled (404). */
export function isUsageAgreementDisabled(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404;
}

/** Read the current agreement state (language, text, accepted). */
export function useUsageAgreementQuery() {
  return useQuery(usageAgreementQueryOptions());
}

/**
 * Accept the current agreement via `POST /api/usage-agreement/accept`, then
 * invalidate the agreement query so it refetches with `accepted: true`.
 */
export function useAcceptUsageAgreement() {
  const queryClient = useQueryClient();
  return useMutation({
    ...acceptUsageAgreementMutation(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getUsageAgreementQueryKey() });
    },
  });
}
