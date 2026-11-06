import { StatusBadge } from "@/components/ui/StatusBadge";
import type { JobStatus } from "@/services/api/jobs/job.types";
import { jobStatusLabels } from "../utils/jobFormat";
export function JobStatusChip({ status }: { status: JobStatus }) {
  return (
    <StatusBadge
      label={jobStatusLabels[status] ?? "Desconhecido"}
      tone={
        status === "Success"
          ? "success"
          : status === "Failed"
            ? "error"
            : status === "Processing"
              ? "info"
              : "default"
      }
    />
  );
}
