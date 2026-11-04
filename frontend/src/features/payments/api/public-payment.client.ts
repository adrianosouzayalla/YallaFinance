"use client";

import { request } from "@/services/api/api-client";
import type {
  PublicPaymentAttempt,
  PublicPaymentCheckout,
  PublicPaymentCheckoutStatus,
  PublicCardPaymentInput,
} from "../types/payment.types";

export const publicPaymentApi = {
  startAttempt: (token: string, methodCode: string, idempotencyKey: string, card?: PublicCardPaymentInput) =>
    request<PublicPaymentAttempt>(`/api/payment/${encodeURIComponent(token)}/attempts`, {
      method: "POST",
      body: JSON.stringify({ methodCode, ...(card ? { card } : {}) }),
      headers: { "Idempotency-Key": idempotencyKey },
    }),
  getStatus: (token: string) =>
    request<PublicPaymentCheckoutStatus>(`/api/payment/${encodeURIComponent(token)}/status`),
};

export type { PublicPaymentAttempt, PublicPaymentCheckout, PublicPaymentCheckoutStatus };
