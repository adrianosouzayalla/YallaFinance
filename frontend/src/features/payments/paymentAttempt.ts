import type { PaymentBatchInput } from "./types/payment.types";
import type { PaymentRequestFormData } from "./schemas/payment-request.schema";
import { paymentDateOnly, paymentExpirationAt } from "./paymentDate";
import { createSecureUuidV4 } from "@/utils/secureUuid";

export function buildPaymentBatchInput(ids: string[], form: PaymentRequestFormData, previous?: PaymentBatchInput): PaymentBatchInput {
  const paymentMethodIds = [...form.paymentMethodIds].sort();
  const stableIds = [...ids].sort();
  if (previous && JSON.stringify(previous.items.map((item) => item.chargeId).sort()) === JSON.stringify(stableIds)
      && JSON.stringify([...previous.paymentMethodIds].sort()) === JSON.stringify(paymentMethodIds)
      && paymentDateOnly(previous.expiresAt) === form.expiresDate && previous.note === form.note) {
    return previous;
  }
  return { items: stableIds.map((chargeId) => ({ chargeId, idempotencyKey: createSecureUuidV4() })), paymentMethodIds,
    expiresAt: paymentExpirationAt(form.expiresDate), note: form.note };
}

export function paymentStorageKey(contextId: string, ids: string[]): string {
  return `payment-intent:${contextId}:${[...ids].sort().join(",")}`;
}
