export type JobStatus = "Pending" | "Processing" | "Success" | "Failed";
export interface JobTypeOption {
  type: string;
  jobTitle: string;
}
export interface JobReprocessingReceipt {
  message: string;
}
export interface JobFilters {
  startDate?: string;
  endDate?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  key: string;
  type: string;
  status: JobStatus | "";
  page: number;
  pageSize: number;
}
export interface JobItem {
  jobTitle: string;
  id: string;
  type: string;
  key: string;
  status: JobStatus;
  createdAt: string;
  updatedAt: string | null;
  startedAt: string | null;
  finishedAt: string | null;
  durationSeconds: number | null;
}
export interface JobPage {
  items: JobItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  summary: {
    pendingCount: number;
    processingCount: number;
    successCount: number;
    failedCount: number;
  };
}
export interface JobDetail {
  jobTitle: string;
  id: string;
  type: string;
  key: string;
  status: JobStatus;
  createdAt: string;
  updatedAt: string | null;
  payload: unknown;
}
export interface JobExecution {
  id: string;
  startedAt: string;
  finishedAt: string | null;
  durationSeconds: number;
  status: JobStatus;
  errorCode: string | null;
  result: unknown;
  details: unknown;
}
export interface JobExecutionPage {
  items: JobExecution[];
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  totalItems: number;
}
