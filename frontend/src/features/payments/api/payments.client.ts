"use client";
import { request } from "@/services/api/api-client";
import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";
import type { PaymentChargeFilters, PaymentChargeList, PaymentPreparation, PaymentBatchInput, PaymentBatchResult, PaymentRequest } from "../types/payment.types";

export const paymentsApi = {
  list: (filters: PaymentChargeFilters, options: SessionApiOptions) => {
    const params = new URLSearchParams({ page: String(filters.page), pageSize: String(filters.pageSize) });
    if (filters.startDate) params.set("startDate", filters.startDate);
    if (filters.endDate) params.set("endDate", filters.endDate);
    if (filters.sortBy) params.set("sortBy", filters.sortBy);
    if (filters.sortDirection) params.set("sortDirection", filters.sortDirection);
    if (filters.paymentStatus) params.set("paymentStatus", filters.paymentStatus);
    if (filters.reference) params.set("reference", filters.reference);
    return request<PaymentChargeList>(`/api/payments/charges?${params}`, sessionRequestOptions(options));
  },
  prepare: (ids: string[], options: SessionApiOptions) => request<PaymentPreparation>("/api/payments/prepare", {
    ...sessionRequestOptions(options), method: "POST", body: JSON.stringify(ids),
  }),
  generate: (data: PaymentBatchInput, options: SessionApiOptions) => request<PaymentBatchResult>("/api/payments/payment-requests/batch", {
    ...sessionRequestOptions(options), method: "POST", body: JSON.stringify(data),
  }),
  getRequest: (id: string, options: SessionApiOptions) => request<PaymentRequest>(`/api/payments/payment-requests/${encodeURIComponent(id)}`, sessionRequestOptions(options)),
  link: (id: string, options: SessionApiOptions) => request<{ id: string; url: string | null }>(`/api/payments/payment-requests/${encodeURIComponent(id)}/link`, sessionRequestOptions(options)),
  cancel: (id: string, options: SessionApiOptions) => request<PaymentRequest>(`/api/payments/payment-requests/${encodeURIComponent(id)}/cancel`, { ...sessionRequestOptions(options), method: "POST" }),
};
