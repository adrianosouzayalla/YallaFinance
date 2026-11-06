import { useEffect, useRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { jobsApi } from "@/services/api/jobs/jobsApi.client";
import { useJobMonitoringVisibility } from "./useJobMonitoringVisibility";
import type { JobStatus } from "@/services/api/jobs/job.types";
export function useJobExecutions(
  id: string,
  page: number,
  status?: JobStatus,
  sort?: { key: string; direction: "asc" | "desc" },
) {
  const { tenantId, contextId } = useSessionContext();
  const visible = useJobMonitoringVisibility();
  const client = useQueryClient();
  const running = status === "Pending" || status === "Processing";
  const previous = useRef(running);
  const finalRefreshPending = useRef(false);
  const query = useQuery({
    queryKey: ["job-executions", tenantId, contextId, id, page, sort],
    enabled: Boolean(status),
    queryFn: ({ signal }) => jobsApi.executions(id, page, { signal, contextId }, sort),
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: (query) =>
      visible && running && query.state.fetchStatus === "idle" ? 10000 : false,
  });
  useEffect(() => {
    if (previous.current && status && !running) finalRefreshPending.current = true;
    previous.current = running;
    // A completion snapshot must be read after any older request finishes.
    if (finalRefreshPending.current && query.fetchStatus === "idle") {
      finalRefreshPending.current = false;
      void client.invalidateQueries(
        { queryKey: ["job-executions", tenantId, contextId, id] },
        { cancelRefetch: false },
      );
    }
  }, [running, status, query.fetchStatus, client, tenantId, contextId, id]);
  return query;
}
