import { sessionRequestOptions, type SessionApiOptions } from "@/services/api/sessionOptions";

import type { DashboardSummary } from "@/services/api/dashboard/dashboard.types";
import { request } from "@/services/api/apiClient";

export const dashboardApi = {
  async getDashboardSummary(options: SessionApiOptions): Promise<DashboardSummary> {
    return request<DashboardSummary>("/api/dashboard/summary", sessionRequestOptions(options));
  },
};
