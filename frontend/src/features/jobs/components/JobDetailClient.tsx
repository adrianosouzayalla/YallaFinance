"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { useRef, useState } from "react";
import Link from "next/link";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { ApiError } from "@/services/api/api-error";
import { useJob } from "../hooks/useJob";
import { useJobExecutions } from "../hooks/useJobExecutions";
import { JobStatusChip } from "./JobStatusChip";
import { formatJobDate } from "../utils/job-format";
import { JobJsonPanel } from "./JobJsonPanel";
import { JobExecutionsPanel } from "./JobExecutionsPanel";
import { JobDetailField } from "./JobDetailField";
import ReplayIcon from "@mui/icons-material/Replay";
import RefreshIcon from "@mui/icons-material/Refresh";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useReprocessJob } from "../hooks/useReprocessJob";
import { JobReprocessDialog } from "./JobReprocessDialog";
import { LoadingState } from "@/components/ui/LoadingState";
export function JobDetailClient({ jobId }: { jobId: string }) {
  const query = useJob(jobId);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<{ key: string; direction: "asc" | "desc" }>({ key: "started", direction: "desc" });
  const executions = useJobExecutions(jobId, page, query.data?.status, sort);
  const reprocess = useReprocessJob(jobId);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [manualLoad, setManualLoad] = useState(false);
  const [manualAction, setManualAction] = useState(false);
  if (manualLoad && !executions.isFetching) setManualLoad(false);
  const [feedback, setFeedback] = useState<{
    severity: "success" | "error";
    message: string;
  } | null>(null);
  const submitting = useRef(false);
  const confirm = async () => {
    if (submitting.current || reprocess.isPending) return;
    setManualAction(true);
    submitting.current = true;
    setFeedback(null);
    try {
      const receipt = await reprocess.mutateAsync();
      setFeedback({ severity: "success", message: receipt.message });
    } catch (error) {
      setFeedback({
        severity: "error",
        message:
          error instanceof ApiError && error.status === 409
            ? error.message
            : "Não foi possível confirmar o reprocessamento. Atualize os dados para verificar o estado do Job.",
      });
    } finally {
      submitting.current = false;
      setConfirmOpen(false);
      setManualAction(false);
    }
  };
  const refresh = async (executionsOnly = false) => {
    setManualAction(true);
    try {
      await Promise.allSettled([
        ...(!executionsOnly ? [query.refetch({ cancelRefetch: false })] : []),
        ...(query.data ? [executions.refetch({ cancelRefetch: false })] : []),
      ]);
    } finally {
      setManualAction(false);
    }
  };
  if (query.isPending || (query.isFetching && !query.data)) return <LoadingState mode="blocking" message="Carregando Job..." />;
  if (!query.data)
    return (
      <Stack spacing={2}>
        <FinancePageTitle navigationHref="/jobs" variant="h4">
          {query.error instanceof ApiError && query.error.status === 404
            ? "Job não encontrado"
            : "Não foi possível consultar o Job."}
        </FinancePageTitle>
        <Button component={Link} href="/jobs" startIcon={<ArrowBackIcon />}>
          Voltar para Jobs
        </Button>
        <Button onClick={() => void refresh()} disabled={query.isFetching}>
          Tentar novamente
        </Button>
      </Stack>
    );
  const job = query.data;
  return (
    <>
    <LoadingState mode="blocking" open={manualAction || manualLoad || executions.isPending} message={reprocess.isPending ? "Reprocessando Job..." : "Atualizando detalhes do Job..."} />
    <Stack spacing={3}>
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
        <Box>
          <FinancePageTitle
            navigationHref="/jobs"
          >
            Detalhe do Job
          </FinancePageTitle>
          <Typography color="text.secondary">Execuções e resultados registrados.</Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Button component={Link} href="/jobs" startIcon={<ArrowBackIcon />}>
            Voltar
          </Button>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            disabled={query.isFetching || executions.isFetching || reprocess.isPending}
            onClick={() => void refresh()}
          >
            Atualizar
          </Button>
          {job.status === "Failed" && (
            <Button
              variant="contained"
              startIcon={<ReplayIcon />}
              disabled={reprocess.isPending}
              onClick={() => {
                setFeedback(null);
                setConfirmOpen(true);
              }}
            >
              Reprocessar
            </Button>
          )}
        </Stack>
      </Stack>
      {feedback && <Alert severity={feedback.severity}>{feedback.message}</Alert>}
      <JobReprocessDialog
        open={confirmOpen}
        pending={reprocess.isPending}
        onClose={() => setConfirmOpen(false)}
        onConfirm={() => void confirm()}
      />
      {query.isError && (
        <Alert severity="error">
          Não foi possível atualizar o Job. Os dados anteriores foram mantidos.
        </Alert>
      )}
      <PrototypePanel title="Job">
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <JobDetailField label="Status">
            <JobStatusChip status={job.status} />
          </JobDetailField>
          <JobDetailField label="Key">{job.key}</JobDetailField>
          <JobDetailField label="Tipo">
            {job.jobTitle && job.jobTitle !== job.type ? `${job.jobTitle} - ${job.type}` : job.type}
          </JobDetailField>
          <JobDetailField label="Criado em">{formatJobDate(job.createdAt)}</JobDetailField>
          <JobDetailField label="Atualizado em">{formatJobDate(job.updatedAt)}</JobDetailField>
          <JobDetailField label="Acompanhamento">
            {job.status === "Pending" || job.status === "Processing"
              ? "Atualização automática a cada 10 segundos com a página visível."
              : "Execução finalizada."}
          </JobDetailField>
        </Stack>
      </PrototypePanel>
      <JobJsonPanel
        title="Payload"
        value={job.payload}
        emptyMessage="Payload indisponível ou inválido."
      />
      <JobExecutionsPanel
        sortBy={sort?.key} sortDirection={sort?.direction} onSort={(key) => {
          setManualLoad(true);
          setSort({ key, direction: sort?.key === key && sort.direction === "asc" ? "desc" : "asc" }); setPage(1);
        }}
        data={executions.data}
        page={page}
        onPageChange={(nextPage) => { setManualLoad(true); setPage(nextPage); }}
        error={executions.isError}
        onRefresh={() => void refresh(true)}
      />
    </Stack>
    </>
  );
}
