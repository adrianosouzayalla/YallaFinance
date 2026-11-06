import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { jobsApi } from "@/services/api/jobs/jobsApi.client";
import { useJobMonitoringVisibility } from "./useJobMonitoringVisibility";
import type { JobFilters } from "@/services/api/jobs/job.types";
export function useJobs(filters: JobFilters) {
  const { tenantId, contextId } = useSessionContext();
  const visible = useJobMonitoringVisibility();
  return useQuery({
    queryKey: ["jobs", tenantId, contextId, filters],
    queryFn: ({ signal }) => jobsApi.list(filters, { signal, contextId }),
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: (query) => (visible && query.state.fetchStatus === "idle" ? 10000 : false),
  });
}
