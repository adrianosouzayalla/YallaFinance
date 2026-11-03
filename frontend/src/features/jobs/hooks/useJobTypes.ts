"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { jobsApi } from "../api/jobs.client";
import { useJobMonitoringVisibility } from "./useJobMonitoringVisibility";
export function useJobTypes() {
  const { tenantId, contextId } = useSessionContext();
  const visible = useJobMonitoringVisibility();
  return useQuery({
    queryKey: ["job-types", tenantId, contextId],
    queryFn: ({ signal }) => jobsApi.types({ signal, contextId }),
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: (query) => (visible && query.state.fetchStatus === "idle" ? 10000 : false),
  });
}
