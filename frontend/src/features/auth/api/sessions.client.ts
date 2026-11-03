"use client";
import { request } from "@/services/api/api-client";
import { sessionRequestOptions, type SessionApiOptions } from "./session-options";

export type SessionSort = "name" | "email" | "createdAt" | "expiresAt";
export interface ActiveSessionItem {
  rowKey: string;
  displayName: string;
  email?: string | null;
  createdAt: string;
  expiresAt: string;
  isCurrent: boolean;
}
export interface ActiveSessionPage {
  items: ActiveSessionItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export interface SessionFilters {
  page: number;
  pageSize: number;
  sortBy: SessionSort;
  sortDirection: "asc" | "desc";
}
export const sessionsApi = {
  list(filters: SessionFilters, options: SessionApiOptions) {
    const query = new URLSearchParams({ page: String(filters.page), pageSize: String(filters.pageSize),
      sortBy: filters.sortBy, sortDirection: filters.sortDirection });
    return request<ActiveSessionPage>(`/api/auth/sessions?${query}`, sessionRequestOptions(options));
  },
};
