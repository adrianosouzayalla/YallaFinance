import { sessionRequestOptions, type SessionApiOptions } from "@/services/api/sessionOptions";

import type { CategoryOption } from "@/services/api/catalogs/catalog.types";
import { request } from "@/services/api/apiClient";

export const categoriesApi = {
  async listCategories(options: SessionApiOptions): Promise<CategoryOption[]> {
    return request<CategoryOption[]>("/api/categories", sessionRequestOptions(options));
  },
};
