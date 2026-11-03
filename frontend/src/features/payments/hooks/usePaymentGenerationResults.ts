"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { paymentsApi } from "../api/payments.client";

export function usePaymentGenerationResults(ids: string[]) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({ queryKey: ["payment-results", tenantId, contextId, ids], enabled: ids.length > 0,
    queryFn: async ({ signal }) => {
      const requests = [];
      for (const id of ids) requests.push(await paymentsApi.getRequest(id, { contextId, signal }));
      const preparation = await paymentsApi.prepare([...new Set(requests.map((row) => row.chargeId))], { contextId, signal });
      return requests.map((request) => ({ ...request, charge: preparation.charges.find((charge) => charge.id === request.chargeId)! }));
    },
  });
}
