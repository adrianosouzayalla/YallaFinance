"use client";

import { request } from "@/services/api/api-client";
import { ApiError } from "@/services/api/api-error";
import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";
import type {
  FinancialCatalogPage,
  FinancialCostCenter,
  FinancialCustomer,
} from "../types/catalog.types";

function invalidCatalog(): never {
  throw new ApiError(
    "INVALID_FINANCIAL_CATALOG",
    "Não foi possível confirmar os dados do catálogo financeiro. Atualize e tente novamente.",
    502,
  );
}

function readPage<T extends FinancialCostCenter>(value: unknown): FinancialCatalogPage<T> {
  const page = value as Partial<FinancialCatalogPage<T>> | null;
  if (
    !page ||
    typeof page.connectionId !== "string" ||
    !page.connectionId.trim() ||
    !Array.isArray(page.items) ||
    !Number.isSafeInteger(page.totalCount) ||
    page.totalCount! < page.items.length ||
    !page.items.every(
      (item) =>
        item &&
        typeof item.externalReference === "string" &&
        item.externalReference.trim() &&
        typeof item.name === "string" &&
        item.name.trim(),
    )
  )
    invalidCatalog();
  return page as FinancialCatalogPage<T>;
}

export const financialCatalogApi = {
  async findCustomersByDocument(
    document: string,
    options: SessionApiOptions,
  ): Promise<FinancialCatalogPage<FinancialCustomer>> {
    const digits = document.replace(/\D/g, "");
    if (!/^(\d{11}|\d{14})$/.test(digits))
      throw new ApiError("INVALID_DOCUMENT", "Informe um CPF ou CNPJ completo.", 400);
    const result = readPage<FinancialCustomer>(
      await request<unknown>("/api/financial-catalog/customers/lookup", {
        method: "POST",
        ...sessionRequestOptions(options),
        body: JSON.stringify({ document: digits }),
      }),
    );
    if (
      result.totalCount !== result.items.length ||
      result.items.some(
        (item) =>
          item.document != null &&
          (typeof item.document !== "string" || item.document.replace(/\D/g, "") !== digits),
      )
    )
      invalidCatalog();
    if (
      result.items.some(
        (item) =>
          (item.email != null && typeof item.email !== "string") ||
          (item.phone != null && typeof item.phone !== "string") ||
          (item.documentType != null &&
            item.documentType !== "CPF" &&
            item.documentType !== "CNPJ"),
      )
    )
      invalidCatalog();
    return result;
  },
  async listCostCenters(
    options: SessionApiOptions,
  ): Promise<FinancialCatalogPage<FinancialCostCenter>> {
    const items: FinancialCostCenter[] = [];
    const references = new Set<string>();
    let connectionId: string | undefined;
    let totalCount: number | undefined;
    do {
      options.signal?.throwIfAborted();
      const page = readPage<FinancialCostCenter>(
        await request<unknown>(
          `/api/financial-catalog/cost-centers?skip=${items.length}&take=500`,
          sessionRequestOptions(options),
        ),
      );
      if (
        (connectionId !== undefined && connectionId !== page.connectionId) ||
        (totalCount !== undefined && totalCount !== page.totalCount) ||
        (page.items.length === 0 && items.length < page.totalCount) ||
        items.length + page.items.length > page.totalCount
      )
        invalidCatalog();
      connectionId = page.connectionId;
      totalCount = page.totalCount;
      for (const item of page.items) {
        if (references.has(item.externalReference)) invalidCatalog();
        references.add(item.externalReference);
        items.push(item);
      }
    } while (items.length < totalCount!);
    return { connectionId: connectionId!, totalCount: totalCount!, items };
  },
};
