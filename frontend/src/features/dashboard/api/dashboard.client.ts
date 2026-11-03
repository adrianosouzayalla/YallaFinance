"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import type { DashboardSummary } from "@/features/dashboard/types/dashboard.types";
import { request } from "@/services/api/api-client";

export const dashboardApi = {
  async getDashboardSummary(options: SessionApiOptions): Promise<DashboardSummary> {
    return request<DashboardSummary>("/api/dashboard/summary", sessionRequestOptions(options));
  },
};
