"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { jobsApi } from "../api/jobs.client";
import { useJobMonitoringVisibility } from "./useJobMonitoringVisibility";
export function useJob(id: string) {
  const { tenantId, contextId } = useSessionContext();
  const visible = useJobMonitoringVisibility();
  return useQuery({
    queryKey: ["job", tenantId, contextId, id],
    queryFn: ({ signal }) => jobsApi.detail(id, { signal, contextId }),
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: (query) =>
      visible &&
      query.state.fetchStatus === "idle" &&
      query.state.data &&
      ["Pending", "Processing"].includes(query.state.data.status)
        ? 10000
        : false,
  });
}
