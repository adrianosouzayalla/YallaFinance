"use client";
import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Alert, Button, Stack, Typography } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { LoadingState } from "@/components/ui/LoadingState";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { SelectablePaginatedTable, type SelectableTableColumn } from "@/components/ui/SelectablePaginatedTable";
import { formatDateTime } from "@/utils/date";
import { financeTokens } from "@/theme/designTokens";
import { useSessionContext } from "../context/SessionProvider";
import { sessionsApi, type ActiveSessionItem, type SessionFilters, type SessionSort } from "../api/sessions.client";

const columns: SelectableTableColumn<ActiveSessionItem, SessionSort>[] = [
  { id: "name", header: "Usuário", render: (s) => s.displayName },
  { id: "email", header: "E-mail", render: (s) => s.email || "—" },
  { id: "createdAt", header: "Início", render: (s) => formatDateTime(s.createdAt) },
  { id: "expiresAt", header: "Expiração", render: (s) => formatDateTime(s.expiresAt) },
  { id: "current", header: "Sessão", sortable: false,
    render: (s) => s.isCurrent ? <StatusBadge label="Sessão atual" tone="info" /> : "—" },
];

export function ActiveSessionsPage() {
  const { tenantId, contextId } = useSessionContext();
  const [filters, setFilters] = useState<SessionFilters>({ page: 1, pageSize: 20, sortBy: "createdAt", sortDirection: "desc" });
  const [manualLoad, setManualLoad] = useState(false);
  const query = useQuery({
    queryKey: ["active-sessions", tenantId, contextId, filters],
    queryFn: ({ signal }) => sessionsApi.list(filters, { contextId, signal }),
    placeholderData: keepPreviousData, retry: false, refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });
  if (manualLoad && !query.isFetching) setManualLoad(false);
  const refresh = () => { setManualLoad(true); void query.refetch(); };
  const change = (next: SessionFilters) => { setManualLoad(true); setFilters(next); };
  return <Stack spacing={3}>
    <LoadingState mode="blocking" open={query.isPending || (manualLoad && query.isFetching)} message="Consultando sessões..." />
    <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}>
      <FinancePageTitle navigationHref="/sessions">Sessões ativas</FinancePageTitle>
      <Button variant="outlined" startIcon={<RefreshIcon />} onClick={refresh}>Atualizar</Button>
    </Stack>
    <Typography color="text.secondary">Sessões válidas da empresa selecionada. Uma sessão ativa não indica presença online.</Typography>
    {query.isError && <Alert severity="error" action={<Button color="inherit" onClick={refresh}>Tentar novamente</Button>}>
      Não foi possível consultar as sessões. Tente novamente.
    </Alert>}
    <PrototypePanel title="Sessões da empresa">
      <SelectablePaginatedTable<ActiveSessionItem, SessionSort>
        headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
        rows={query.data?.items ?? []} columns={columns} showSelection={false}
        getRowId={(s) => s.rowKey}
        selectedRows={[]} onSelectionChange={() => { }} tableLabel="Sessões ativas da empresa"
        emptyContent={query.isError ? "Consulta indisponível." : "Nenhuma sessão ativa encontrada."}
        sortBy={filters.sortBy} sortDirection={filters.sortDirection}
        onSort={(key) => change({ ...filters, page: 1, sortBy: key,
          sortDirection: filters.sortBy === key && filters.sortDirection === "asc" ? "desc" : "asc" })}
        pagination={{ page: filters.page, pageSize: filters.pageSize, totalItems: query.data?.totalCount ?? 0,
          onPageChange: (page) => change({ ...filters, page }) }} />
    </PrototypePanel>
  </Stack>;
}
