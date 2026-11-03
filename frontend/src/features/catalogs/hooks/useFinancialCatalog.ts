"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { financialCatalogApi } from "../api/financial-catalog.client";

export function useFinancialCostCenters() {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["financial-cost-centers", tenantId, contextId],
    queryFn: ({ signal }) => financialCatalogApi.listCostCenters({ contextId, signal }),
  });
}

export function useFinancialCustomerLookup() {
  const { tenantId, contextId } = useSessionContext();
  return useMutation({
    mutationKey: ["financial-customer-lookup", tenantId, contextId],
    retry: false,
    mutationFn: ({ document, signal }: { document: string; signal: AbortSignal }) =>
      financialCatalogApi.findCustomersByDocument(document, { contextId, signal }),
  });
}
