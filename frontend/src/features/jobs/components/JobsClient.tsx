"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { useState } from "react";
import Link from "next/link";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { SummaryCard } from "@/components/ui/SummaryCard";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { useJobs } from "../hooks/useJobs";
import { useJobTypes } from "../hooks/useJobTypes";
import type { JobFilters as Filters, JobItem, JobStatus } from "@/services/api/jobs/job.types";
import { JobFilters } from "./JobFilters";
import { JobStatusChip } from "./JobStatusChip";
import { formatJobDate, formatJobDuration } from "../utils/jobFormat";
import RefreshIcon from "@mui/icons-material/Refresh";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import HourglassEmptyOutlinedIcon from "@mui/icons-material/HourglassEmptyOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import ErrorOutlinedIcon from "@mui/icons-material/ErrorOutlined";
import { LoadingState } from "@/components/ui/LoadingState";
import { isValidDateOnly } from "@/utils/date";
function createDefaults(): Filters {
  const now = new Date();
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  return { key: "", type: "", status: "", page: 1, pageSize: 20, startDate: today, endDate: today };
}
const columns: SelectableTableColumn<JobItem>[] = [
  {
    id: "key",
    header: "Key",
    render: (j) => (
      <Typography variant="body2" title={j.key} sx={{ overflowWrap: "anywhere" }}>
        {j.key}
      </Typography>
    ),
  },
  {
    id: "type",
    header: "Tipo",
    render: (j) => (
      <Typography variant="body2" sx={{ overflowWrap: "anywhere" }}>
        {j.jobTitle || j.type}
      </Typography>
    ),
  },
  { id: "status", header: "Status", render: (j) => <JobStatusChip status={j.status} /> },
  { id: "created", header: "Criação", render: (j) => formatJobDate(j.createdAt) },
  { id: "started", header: "Início", render: (j) => formatJobDate(j.startedAt) },
  { id: "duration", header: "Duração", render: (j) => formatJobDuration(j.durationSeconds) },
  {
    id: "detail",
    sortable: false,
    header: "Detalhes",
    render: (j) => (
      <Button
        size="small"
        startIcon={<VisibilityOutlinedIcon />}
        component={Link}
        href={`/jobs/${encodeURIComponent(j.id)}`}
      >
        Ver detalhes
      </Button>
    ),
  },
];
export function JobsClient() {
  const [filters, setFilters] = useState(createDefaults);
  const [draft, setDraft] = useState(filters);
  const [periodError, setPeriodError] = useState(false);
  const query = useJobs(filters);
  const types = useJobTypes();
  const [manualLoad, setManualLoad] = useState(false);
  if (manualLoad && !query.isFetching && !types.isFetching) {
    setManualLoad(false);
  }
  const refresh = () => {
    setManualLoad(true);
    void query.refetch({ cancelRefetch: false });
    void types.refetch({ cancelRefetch: false });
  };
  const filterByStatus = (status: JobStatus) => {
    setManualLoad(true);
    setDraft((current) => ({ ...current, status, page: 1 }));
    if (filters.status === status && filters.page === 1) {
      void query.refetch({ cancelRefetch: false });
    } else {
      setFilters((current) => ({ ...current, status, page: 1 }));
    }
  };
  return (
    <>
      <LoadingState
        mode="blocking"
        open={query.isPending || types.isPending || manualLoad}
        message={manualLoad ? "Atualizando Jobs..." : "Carregando Jobs..."}
      />
      <Stack spacing={3}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
          <Box>
            <FinancePageTitle navigationHref="/jobs">Jobs</FinancePageTitle>
          </Box>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            disabled={query.isFetching}
            onClick={refresh}
          >
            Atualizar
          </Button>
        </Stack>
        {types.isError && (
          <Alert
            severity="error"
            sx={{ mb: 2 }}
            action={
              <Button
                color="inherit"
                onClick={() => {
                  setManualLoad(true);
                  void types.refetch({ cancelRefetch: false });
                }}
              >
                Tentar novamente
              </Button>
            }
          >
            Não foi possível atualizar os tipos de Job.
          </Alert>
        )}
        <JobFilters
          types={types.data ?? []}
          typesLoading={false}
          value={draft}
          onChange={setDraft}
          onSearch={() => {
            if (
              !draft.startDate ||
              !draft.endDate ||
              !isValidDateOnly(draft.startDate) ||
              !isValidDateOnly(draft.endDate) ||
              draft.startDate > draft.endDate
            ) {
              setPeriodError(true);
              return;
            }
            setPeriodError(false);
            setManualLoad(true);
            setFilters({
              ...draft,
              sortBy: filters.sortBy,
              sortDirection: filters.sortDirection,
              key: draft.key.trim(),
              type: draft.type.trim(),
              page: 1,
            });
          }}
          onClear={() => {
            const defaults = createDefaults();
            setPeriodError(false);
            setDraft(defaults);
            setManualLoad(true);
            setFilters(defaults);
          }}
        />
        {periodError && (
          <Alert severity="error">
            Informe um período válido. A data início deve ser menor ou igual à data fim.
          </Alert>
        )}
        <Box
          component="section"
          aria-label="Resumo dos Jobs"
          sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(4, minmax(0, 1fr))" }}
        >
          <SummaryCard
            label="Aguardando"
            icon={<HourglassEmptyOutlinedIcon />}
            color="warning"
            value={query.data?.summary ? String(query.data.summary.pendingCount) : "—"}
            onClick={() => filterByStatus("Pending")}
            selected={filters.status === "Pending"}
          />
          <SummaryCard
            label="Em processamento"
            icon={<SettingsOutlinedIcon />}
            color="info"
            value={query.data?.summary ? String(query.data.summary.processingCount) : "—"}
            onClick={() => filterByStatus("Processing")}
            selected={filters.status === "Processing"}
          />
          <SummaryCard
            label="Concluídos"
            icon={<CheckCircleOutlinedIcon />}
            color="success"
            value={query.data?.summary ? String(query.data.summary.successCount) : "—"}
            onClick={() => filterByStatus("Success")}
            selected={filters.status === "Success"}
          />
          <SummaryCard
            label="Falhas"
            icon={<ErrorOutlinedIcon />}
            color="error"
            value={query.data?.summary ? String(query.data.summary.failedCount) : "—"}
            onClick={() => filterByStatus("Failed")}
            selected={filters.status === "Failed"}
          />
        </Box>
        {query.isError && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" disabled={query.isFetching} onClick={refresh}>
                Tentar novamente
              </Button>
            }
          >
            {query.data
              ? "Não foi possível atualizar os Jobs. Os dados anteriores foram mantidos."
              : "Não foi possível consultar os Jobs."}
          </Alert>
        )}
        <PrototypePanel title="Jobs">
          <SelectablePaginatedTable
            sortBy={filters.sortBy}
            sortDirection={filters.sortDirection}
            onSort={(key) => {
              setManualLoad(true);
              setFilters((current) => ({
                ...current,
                sortBy: key,
                sortDirection:
                  current.sortBy === key && current.sortDirection === "asc" ? "desc" : "asc",
                page: 1,
              }));
            }}
            rows={query.data?.items ?? []}
            columns={columns}
            getRowId={(j) => j.id}
            selectedRows={[]}
            onSelectionChange={() => {}}
            showSelection={false}
            loading={false}
            tableLabel="Jobs"
            emptyContent={
              query.isError && !query.data ? "Consulta indisponível." : "Nenhum Job encontrado."
            }
            pagination={{
              page: filters.page,
              pageSize: filters.pageSize,
              totalItems: query.data?.totalItems ?? 0,
              onPageChange: (page) => {
                setManualLoad(true);
                setFilters((current) => ({ ...current, page }));
              },
            }}
          />
        </PrototypePanel>
      </Stack>
    </>
  );
}
