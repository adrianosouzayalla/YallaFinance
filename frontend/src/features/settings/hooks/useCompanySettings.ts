"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { companyApi } from "../company.client";
import type { CompanyConnectionFilter, CompanySettingsByGroup, CompanySettingsGroup } from "../types";

export function useCompanySettings<T extends CompanySettingsGroup>(group: T, enabled: boolean) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["company", tenantId, contextId, "settings", group],
    queryFn: ({ signal }) => companyApi.getSettings(group, { contextId, signal }),
    enabled: enabled && !!tenantId && !!contextId,
    staleTime: 60_000,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    placeholderData: undefined,
  });
}
export function useCompanyConnections(filters: CompanyConnectionFilter, enabled: boolean) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["company", tenantId, contextId, "connections", filters],
    queryFn: ({ signal }) => companyApi.listConnections(filters, { contextId, signal }),
    enabled: enabled && !!tenantId && !!contextId,
    staleTime: 60_000,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    placeholderData: (previousData, previousQuery) =>
      previousQuery?.queryKey[1] === tenantId && previousQuery.queryKey[2] === contextId
        ? previousData
        : undefined,
  });
}

export function useSaveCompanySettings<T extends CompanySettingsGroup>(group: T) {
  const { tenantId, contextId } = useSessionContext();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (values: Partial<CompanySettingsByGroup[T]>) =>
      companyApi.saveSettings(group, values, { contextId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["company", tenantId, contextId, "settings", group] }),
  });
}
