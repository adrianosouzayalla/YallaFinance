"use client";
import { useCallback, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { debtsApi } from "../api/debts.client";
import type { DebtFilters } from "../types/debt.types";

export function useDebtSearch() {
  const { tenantId, contextId } = useSessionContext();
  const client = useQueryClient();
  const [filters, setFilters] = useState<DebtFilters | null>(null);
  const query = useQuery({
    queryKey: ["debts", tenantId, contextId, filters],
    enabled: filters !== null,
    queryFn: ({ signal }) => debtsApi.listDebts(filters!, { signal, contextId }),
    placeholderData: (previous, previousQuery) =>
      previousQuery?.queryKey[1] === tenantId && previousQuery.queryKey[2] === contextId
        ? previous
        : undefined,
  });
  const load = useCallback(
    (nextFilters: DebtFilters) => {
      const result = client.fetchQuery({
        queryKey: ["debts", tenantId, contextId, nextFilters],
        queryFn: ({ signal }) => debtsApi.listDebts(nextFilters, { signal, contextId }),
      });
      setFilters(nextFilters);
      return result;
    },
    [client, tenantId, contextId],
  );
  return { ...query, load, started: filters !== null };
}
