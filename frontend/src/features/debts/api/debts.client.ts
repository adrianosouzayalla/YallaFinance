"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import type { CategoryOption } from "@/features/catalogs/types/catalog.types";
import type { Debt, DebtFilters, DebtListResult } from "@/features/debts/types/debt.types";
import { ApiError, request } from "@/services/api/api-client";
import type { ManualDebtInput, ManualDebtReceipt } from "../types/manual-debt.types";

export type ApiDebt = Omit<Debt, "category"> & { categoryId: string };

function toDebt(result: ApiDebt, categories: CategoryOption[]): Debt {
  const category = categories.find((item) => item.id === result.categoryId);
  return { ...result, category: (category?.name ?? "") as Debt["category"] };
}

function query(filters: DebtFilters): string {
  const params = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(filters.pageSize),
    sortDirection: filters.sortDirection,
  });
  if (filters.status !== "Todos") params.set("status", filters.status);
  const categories = filters.categories.filter((category) => category.trim().length > 0);
  if (categories.length) params.set("categories", categories.join(","));
  if (filters.startDate) params.set("startDate", filters.startDate);
  if (filters.endDate) params.set("endDate", filters.endDate);
  if (filters.sortBy) params.set("sortBy", filters.sortBy);
  return params.toString();
}

export const debtsApi = {
  createManualDebt: (input: ManualDebtInput, options: SessionApiOptions) =>
    request<ManualDebtReceipt>("/api/debts", {
      ...sessionRequestOptions(options),
      method: "POST",
      body: JSON.stringify(input),
    }),
  async getDebts(ids: string[], options: SessionApiOptions): Promise<Debt[]> {
    const uniqueIds = [...new Set(ids)];
    if (uniqueIds.length === 0) return [];
    const categories = await request<CategoryOption[]>(
      "/api/categories",
      sessionRequestOptions(options),
    );
    const items: Debt[] = [];
    // Bound simultaneous requests; work scales with the selection, never the catalog.
    for (let offset = 0; offset < uniqueIds.length; offset += 10) {
      options.signal?.throwIfAborted();
      const batch = await Promise.all(
        uniqueIds.slice(offset, offset + 10).map(async (id) => {
          const result = await debtsApi.getRawDebt(id, options);
          return toDebt(result, categories);
        }),
      );
      items.push(...batch);
    }
    return items;
  },
  getRawDebt: (id: string, options: SessionApiOptions) =>
    request<ApiDebt>(`/api/debts/${encodeURIComponent(id)}`, sessionRequestOptions(options)),
  async listDebts(filters: DebtFilters, options: SessionApiOptions): Promise<DebtListResult> {
    const [result, categories] = await Promise.all([
      request<Omit<DebtListResult, "items"> & { items: ApiDebt[] }>(
        `/api/debts?${query(filters)}`,
        sessionRequestOptions(options),
      ),
      request<CategoryOption[]>("/api/categories", sessionRequestOptions(options)),
    ]);
    return { ...result, items: result.items.map((item) => toDebt(item, categories)) };
  },
  async getDebt(id: string, options: SessionApiOptions): Promise<Debt | null> {
    try {
      const [result, categories] = await Promise.all([
        request<ApiDebt>(`/api/debts/${encodeURIComponent(id)}`, sessionRequestOptions(options)),
        request<CategoryOption[]>("/api/categories", sessionRequestOptions(options)),
      ]);
      return toDebt(result, categories);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) return null;
      throw error;
    }
  },
};
