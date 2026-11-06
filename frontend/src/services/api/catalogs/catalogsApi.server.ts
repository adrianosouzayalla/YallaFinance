import "server-only";
import { backendResponse } from "@/services/api/apiClient.server";

export const catalogsServerApi = {
  financialCustomers: (body: string, token: string, signal?: AbortSignal) =>
    backendResponse("/api/financial-catalog/customers/lookup", token, {
      method: "POST",
      body,
      signal,
    }),
  financialCostCenters: (skip: number, take: number, token: string, signal?: AbortSignal) =>
    backendResponse(`/api/financial-catalog/cost-centers?skip=${skip}&take=${take}`, token, {
      signal,
    }),
  categories: (token: string, signal?: AbortSignal) =>
    backendResponse("/api/categories", token, { signal }),
};
