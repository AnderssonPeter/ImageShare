import {
  acceptUsageAgreementMutation,
  getUsageAgreementOptions,
  getUsageAgreementQueryKey,
} from "@lib/api/generated/@tanstack/react-query.gen";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@lib/api/errors";

export function usageAgreementQueryOptions() {
  return {
    ...getUsageAgreementOptions(),
    retry: (failureCount: number, error: unknown) =>
      !(error instanceof ApiError && error.status === 404) && failureCount < 3,
  };
}

export function isUsageAgreementDisabled(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404;
}

export function useUsageAgreementQuery() {
  return useQuery(usageAgreementQueryOptions());
}

export function useCanLoadImages(): boolean {
  const { data, error, isLoading } = useUsageAgreementQuery();
  return !isLoading && (isUsageAgreementDisabled(error) || data?.accepted === true);
}

export function useAcceptUsageAgreement() {
  const queryClient = useQueryClient();
  return useMutation({
    ...acceptUsageAgreementMutation(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getUsageAgreementQueryKey() });
    },
  });
}
