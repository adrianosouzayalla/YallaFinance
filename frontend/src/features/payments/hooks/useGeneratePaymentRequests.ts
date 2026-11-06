import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { paymentsApi } from "@/services/api/payments/paymentsApi.client";
import type { PaymentBatchInput } from "@/services/api/payments/payment.types";

export function useGeneratePaymentRequests() {
  const { contextId } = useSessionContext();
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (data: PaymentBatchInput) => paymentsApi.generate(data, { contextId }),
    onSuccess: () => {
      void cache.invalidateQueries({ queryKey: ["payment-charges"] });
      void cache.invalidateQueries({ queryKey: ["payment-preparation"] });
    },
  });
}
