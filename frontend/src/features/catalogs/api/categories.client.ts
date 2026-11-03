"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import type { CategoryOption } from "@/features/catalogs/types/catalog.types";
import { request } from "@/services/api/api-client";

export const categoriesApi = {
  async listCategories(options: SessionApiOptions): Promise<CategoryOption[]> {
    return request<CategoryOption[]>("/api/categories", sessionRequestOptions(options));
  },
};
