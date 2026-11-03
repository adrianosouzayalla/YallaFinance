"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { paymentsApi } from "../api/payments.client";

export function usePaymentPreparation(chargeIds: string[]) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({ queryKey: ["payment-preparation", tenantId, contextId, chargeIds], enabled: chargeIds.length > 0,
    queryFn: ({ signal }) => paymentsApi.prepare(chargeIds, { contextId, signal }), staleTime: 0,
  });
}
