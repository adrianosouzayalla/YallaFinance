import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { ApiError } from "@/services/api/apiError";
import { jobsApi } from "@/services/api/jobs/jobsApi.client";

export function useReprocessJob(id: string) {
  const { tenantId, contextId } = useSessionContext();
  const client = useQueryClient();
  return useMutation({
    mutationKey: ["reprocess-job", tenantId, contextId, id],
    mutationFn: () => jobsApi.reprocess(id, { contextId }),
    retry: false,
    onMutate: () => ({ tenantId, contextId, id }),
    onSettled: async (_data, error, _variables, snapshot) => {
      if (!snapshot || (error && !(error instanceof ApiError && error.status === 409))) return;
      const keys = [
        ["job", snapshot.tenantId, snapshot.contextId, snapshot.id],
        ["jobs", snapshot.tenantId, snapshot.contextId],
        ["job-executions", snapshot.tenantId, snapshot.contextId, snapshot.id],
      ];
      // Discard GETs started before the POST, then request fresh snapshots.
      await Promise.all(keys.map((queryKey) => client.cancelQueries({ queryKey })));
      await Promise.all(
        keys.map((queryKey) => client.invalidateQueries({ queryKey }, { cancelRefetch: false })),
      );
    },
  });
}
