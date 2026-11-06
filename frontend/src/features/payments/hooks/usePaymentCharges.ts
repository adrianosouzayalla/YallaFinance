import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { paymentsApi } from "@/services/api/payments/paymentsApi.client";
import type { PaymentChargeFilters } from "@/services/api/payments/payment.types";

export function usePaymentCharges(filters: PaymentChargeFilters) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["payment-charges", tenantId, contextId, filters],
    queryFn: ({ signal }) => paymentsApi.list(filters, { contextId, signal }),
    placeholderData: (previousData, previousQuery) =>
      previousQuery?.queryKey[1] === tenantId && previousQuery.queryKey[2] === contextId
        ? previousData
        : undefined,
  });
}
