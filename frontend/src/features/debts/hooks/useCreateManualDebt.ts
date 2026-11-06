import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { debtsApi } from "@/services/api/debts/debtsApi.client";
import type { ManualDebtInput } from "@/services/api/debts/manualDebt.types";
export function useCreateManualDebt() {
  const { tenantId, contextId } = useSessionContext();
  const client = useQueryClient();
  return useMutation({
    retry: false,
    mutationFn: (input: ManualDebtInput) => debtsApi.createManualDebt(input, { contextId }),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: ["debts", tenantId, contextId] });
      void client.invalidateQueries({ queryKey: ["dashboard", tenantId, contextId] });
    },
  });
}
