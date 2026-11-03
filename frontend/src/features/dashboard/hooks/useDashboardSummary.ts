"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { dashboardApi } from "../api/dashboard.client";
import { usePageVisibility } from "@/hooks/usePageVisibility";
export function useDashboardSummary() {
  const { tenantId, contextId } = useSessionContext();
  const visible = usePageVisibility();
  return useQuery({
    queryKey: ["dashboard", tenantId, contextId],
    queryFn: ({ signal }) => dashboardApi.getDashboardSummary({ signal, contextId }),
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchInterval: (query) => (visible && query.state.fetchStatus === "idle" ? 30000 : false),
  });
}
