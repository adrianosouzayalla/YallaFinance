"use client";
import { useState } from "react";
import { Alert, Button, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { JobStatusChip } from "./JobStatusChip";
import { JobJsonPanel } from "./JobJsonPanel";
import { JobDetailField } from "./JobDetailField";
import { formatJobDate, formatJobDuration, jobErrorMessage } from "../utils/job-format";
import type { JobExecution, JobExecutionPage } from "../types/job.types";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";

function findProviderError(value: unknown): { code?: string; message?: string } | undefined {
  if (Array.isArray(value)) {
    for (const item of value) {
      const found = findProviderError(item);
      if (found) return found;
    }
    return undefined;
  }
  if (!value || typeof value !== "object") return undefined;
  const record = value as Record<string, unknown>;
  const code = typeof record.providerErrorCode === "string" ? record.providerErrorCode : undefined;
  const message = typeof record.providerErrorMessage === "string" ? record.providerErrorMessage : undefined;
  if (code || message) return { code, message };
  for (const child of Object.values(record)) {
    const found = findProviderError(child);
    if (found) return found;
  }
  return undefined;
}

function findManualProviderError(
  value: unknown,
): { code?: string; message?: string } | undefined {
  if (Array.isArray(value)) {
    for (const item of value) {
      const found = findManualProviderError(item);
      if (found) return found;
    }
    return undefined;
  }
  if (!value || typeof value !== "object") return undefined;
  const record = value as Record<string, unknown>;
  if (record.source === "manual-reproduction") {
    return {
      code: typeof record.providerErrorCode === "string" ? record.providerErrorCode : undefined,
      message: typeof record.providerErrorMessage === "string" ? record.providerErrorMessage : undefined,
    };
  }
  for (const child of Object.values(record)) {
    const found = findManualProviderError(child);
    if (found) return found;
  }
  return undefined;
}

export function JobExecutionsPanel({
  data,
  page,
  onPageChange,
  error,
  onRefresh,
  sortBy, sortDirection, onSort,
}: {
  sortBy?: string; sortDirection?: "asc" | "desc"; onSort?(key: string): void;
  data?: JobExecutionPage;
  page: number;
  onPageChange(page: number): void;
  error: boolean;
  onRefresh(): void;
}) {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const selected = data?.items.find((item) => item.id === selectedId) ?? data?.items[0];
  const providerError = findProviderError(selected?.details) ?? findProviderError(selected?.result);
  const manualProviderEvidence = providerError ? undefined : data?.items
    .filter((item) => item.id !== selected?.id)
    .map((item) => ({ executionId: item.id, error: findManualProviderError(item.details) }))
    .find((item) => item.error);
  const columns: SelectableTableColumn<JobExecution>[] = [
    {
      id: "id",
      header: "Execução",
      render: (e) => (
        <Typography variant="body2" sx={{ overflowWrap: "anywhere" }}>
          {e.id}
        </Typography>
      ),
    },
    { id: "status", header: "Status", render: (e) => <JobStatusChip status={e.status} /> },
    { id: "started", header: "Início", render: (e) => formatJobDate(e.startedAt) },
    {
      id: "finished",
      header: "Término",
      render: (e) => (e.finishedAt ? formatJobDate(e.finishedAt) : "Em andamento"),
    },
    { id: "duration", header: "Duração", render: (e) => formatJobDuration(e.durationSeconds) },
    { id: "error", header: "Erro", render: (e) => e.errorCode ?? "—" },
    {
      id: "details",
      sortable: false,
      header: "Detalhes",
      render: (e) => (
        <Button
          size="small"
          startIcon={<VisibilityOutlinedIcon />}
          onClick={() => setSelectedId(e.id)}
        >
          Ver detalhes
        </Button>
      ),
    },
  ];
  return (
    <Stack spacing={3}>
      <PrototypePanel title="Execuções">
        {error && (
          <Alert
            severity="error"
            sx={{ m: 2.5 }}
            action={
              <Button color="inherit" onClick={onRefresh}>
                Tentar novamente
              </Button>
            }
          >
            {data
              ? "Não foi possível atualizar as execuções. Os dados anteriores foram mantidos."
              : "Não foi possível consultar as execuções."}
          </Alert>
        )}
        <SelectablePaginatedTable
          sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}
          rows={data?.items ?? []}
          columns={columns}
          getRowId={(e) => e.id}
          selectedRows={[]}
          onSelectionChange={() => {}}
          showSelection={false}
          loading={false}
          tableLabel="Execuções do Job"
          emptyContent={
            error && !data
                ? "Consulta indisponível."
                : page === 1
                  ? "Ainda não há execução registrada."
                  : "Nenhuma execução nesta página."
          }
          pagination={{
            page,
            pageSize: data?.pageSize ?? 20,
            totalItems: data?.totalItems ?? 0,
            onPageChange,
          }}
        />
      </PrototypePanel>
      {selected && (
        <PrototypePanel title="Detalhes da execução">
          <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
            <JobDetailField label="Execução">{selected.id}</JobDetailField>
            <JobDetailField label="Status">
              <JobStatusChip status={selected.status} />
            </JobDetailField>
            <JobDetailField label="Início">{formatJobDate(selected.startedAt)}</JobDetailField>
            <JobDetailField label="Término">
              {selected.finishedAt ? formatJobDate(selected.finishedAt) : "Em andamento"}
            </JobDetailField>
            <JobDetailField label="Duração">
              {formatJobDuration(selected.durationSeconds)}
            </JobDetailField>
            {selected.errorCode && (
              <Alert severity="error">
                <JobDetailField label="Erro">{jobErrorMessage(selected.errorCode)}</JobDetailField>
                <JobDetailField label="Código">{selected.errorCode}</JobDetailField>
              </Alert>
            )}
            {providerError && (
              <Alert severity="error">
                <Typography sx={{ fontWeight: 600 }}>Erro retornado pelo provedor</Typography>
                {providerError.message && <Typography>{providerError.message}</Typography>}
                {providerError.code && <Typography variant="body2">Código: {providerError.code}</Typography>}
              </Alert>
            )}
            {manualProviderEvidence?.error && (
              <Alert severity="warning">
                <Typography sx={{ fontWeight: 600 }}>Última reprodução manual da integração financeira</Typography>
                {manualProviderEvidence.error.message && <Typography>{manualProviderEvidence.error.message}</Typography>}
                {manualProviderEvidence.error.code && <Typography variant="body2">Código: {manualProviderEvidence.error.code}</Typography>}
                <Typography variant="body2">Execução de origem: {manualProviderEvidence.executionId}</Typography>
              </Alert>
            )}
            <JobJsonPanel
              title="Resultado"
              value={selected.result}
              emptyMessage="Sem resultado registrado."
            />
            <JobJsonPanel
              title="Detalhes registrados"
              value={selected.details}
              emptyMessage="Sem detalhes registrados."
            />
          </Stack>
        </PrototypePanel>
      )}
    </Stack>
  );
}
