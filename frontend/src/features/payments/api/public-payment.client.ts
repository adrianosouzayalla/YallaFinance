"use client";

import { request } from "@/services/api/api-client";
import type { PublicPaymentAttempt, PublicPaymentCheckout, PublicCardPaymentInput } from "../types/payment.types";

export const publicPaymentApi = {
  startAttempt: (token: string, methodCode: string, idempotencyKey: string, card?: PublicCardPaymentInput) =>
    request<PublicPaymentAttempt>(`/api/payment/${encodeURIComponent(token)}/attempts`, {
      method: "POST",
      body: JSON.stringify({ methodCode, ...(card ? { card } : {}) }),
      headers: { "Idempotency-Key": idempotencyKey },
    }),
};

export type { PublicPaymentAttempt, PublicPaymentCheckout };
