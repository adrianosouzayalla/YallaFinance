import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { companyApi } from "@/services/api/settings/settingsApi.client";
import {
  DEFAULT_DETAIL_FILTERS,
  type CompanyConnectionDetailFilter,
} from "@/services/api/settings/types";

export function useCompanyConnection(
  id: string | null,
  filters: Partial<CompanyConnectionDetailFilter>,
  enabled: boolean,
) {
  const { tenantId, contextId } = useSessionContext();
  const requestFilters = { ...DEFAULT_DETAIL_FILTERS, ...filters };
  return useQuery({
    queryKey: ["company", tenantId, contextId, "connection", id, requestFilters],
    queryFn: ({ signal }) => companyApi.getConnection(id!, requestFilters, { contextId, signal }),
    enabled: enabled && !!id && !!tenantId && !!contextId,
    staleTime: 60_000,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    placeholderData: (previousData, previousQuery) =>
      previousQuery?.queryKey[1] === tenantId &&
      previousQuery.queryKey[2] === contextId &&
      previousQuery.queryKey[4] === id
        ? previousData
        : undefined,
  });
}

export function useCompanyCatalog(kind: "categories" | "payment-methods", enabled: boolean) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["company", tenantId, contextId, "catalog", kind],
    queryFn: ({ signal }) =>
      kind === "categories"
        ? companyApi.listCategories({ contextId, signal })
        : companyApi.listPaymentMethods({ contextId, signal }),
    enabled: enabled && !!tenantId && !!contextId,
    staleTime: 60_000,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });
}

export function useInvalidateCompanyConnection(id: string | null) {
  const { tenantId, contextId } = useSessionContext();
  const queryClient = useQueryClient();
  return async () => {
    await queryClient.invalidateQueries({
      queryKey: ["company", tenantId, contextId, "connections"],
    });
    if (id)
      await queryClient.invalidateQueries({
        queryKey: ["company", tenantId, contextId, "connection", id],
      });
  };
}
