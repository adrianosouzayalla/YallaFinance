"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import type { PaymentMethodOption } from "@/features/catalogs/types/catalog.types";
import { request } from "@/services/api/api-client";

export const paymentMethodsApi = {
  async listPaymentMethods(options: SessionApiOptions): Promise<PaymentMethodOption[]> {
    return request<PaymentMethodOption[]>("/api/payment-methods", sessionRequestOptions(options));
  },
};
