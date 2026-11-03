"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { debtsApi } from "../api/debts.client";
export function useDebt(id: string) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["debt", tenantId, contextId, id],
    queryFn: ({ signal }) => debtsApi.getDebt(id, { signal, contextId }),
  });
}
