import { request } from "@/services/api/apiClient";
import { sessionRequestOptions, type SessionApiOptions } from "@/services/api/sessionOptions";
import type {
  JobTypeOption,
  JobFilters,
  JobPage,
  JobDetail,
  JobExecutionPage,
  JobReprocessingReceipt,
} from "@/services/api/jobs/job.types";
export const jobsApi = {
  reprocess(id: string, options: SessionApiOptions) {
    return request<JobReprocessingReceipt>(`/api/jobs/${encodeURIComponent(id)}/reprocess`, {
      ...sessionRequestOptions(options),
      method: "POST",
    });
  },
  types(options: SessionApiOptions) {
    return request<JobTypeOption[]>("/api/jobs/types", sessionRequestOptions(options));
  },
  list(filters: JobFilters, options: SessionApiOptions) {
    const params = new URLSearchParams({
      page: String(filters.page),
      pageSize: String(filters.pageSize),
    });
    if (filters.key.trim()) params.set("key", filters.key.trim());
    if (filters.type.trim()) params.set("type", filters.type.trim());
    if (filters.status) params.set("status", filters.status);
    if (filters.startDate)
      params.set("startDate", new Date(`${filters.startDate}T00:00:00`).toISOString());
    if (filters.endDate) {
      const end = new Date(`${filters.endDate}T00:00:00`);
      end.setDate(end.getDate() + 1);
      params.set("endDate", end.toISOString());
    }
    if (filters.sortBy) params.set("sortBy", filters.sortBy);
    if (filters.sortDirection) params.set("sortDirection", filters.sortDirection);
    return request<JobPage>(`/api/jobs?${params}`, sessionRequestOptions(options));
  },
  detail(id: string, options: SessionApiOptions) {
    return request<JobDetail>(
      `/api/jobs/${encodeURIComponent(id)}`,
      sessionRequestOptions(options),
    );
  },
  executions(
    id: string,
    page: number,
    options: SessionApiOptions,
    sort?: { key: string; direction: "asc" | "desc" },
  ) {
    const params = new URLSearchParams({ page: String(page), pageSize: "20" });
    if (sort) {
      params.set("sortBy", sort.key);
      params.set("sortDirection", sort.direction);
    }
    return request<JobExecutionPage>(
      `/api/jobs/${encodeURIComponent(id)}/executions?${params}`,
      sessionRequestOptions(options),
    );
  },
};
