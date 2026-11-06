import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { debtsApi } from "@/services/api/debts/debtsApi.client";
export function useChargeDebts(debtIds: string[] = []) {
  const { tenantId, contextId } = useSessionContext();
  const ids = [...new Set(debtIds)].sort();
  return useQuery({
    queryKey: ["debts", tenantId, contextId, "charge-selection", ids],
    enabled: ids.length > 0,
    queryFn: async ({ signal }) => {
      const items = await debtsApi.getDebts(ids, { signal, contextId });
      return { items };
    },
  });
}
