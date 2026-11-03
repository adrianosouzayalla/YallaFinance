"use client";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { paymentsApi } from "../api/payments.client";
import type { PaymentBatchInput } from "../types/payment.types";

export function useGeneratePaymentRequests() {
  const { contextId } = useSessionContext(); const cache = useQueryClient();
  return useMutation({ mutationFn: (data: PaymentBatchInput) => paymentsApi.generate(data, { contextId }),
    onSuccess: () => { void cache.invalidateQueries({ queryKey: ["payment-charges"] }); void cache.invalidateQueries({ queryKey: ["payment-preparation"] }); },
  });
}
