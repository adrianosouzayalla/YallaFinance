"use client";
import { useState } from "react";
import Link from "next/link";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Alert, Button, Stack, TextField } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { LoadingState } from "@/components/ui/LoadingState";
import { StatusBadge } from "@/components/ui/StatusBadge";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { directoryQuery } from "@/services/api/directoryApi";
import { accessProfilesApi } from "@/services/api/accessProfiles/accessProfilesApi.client";
import type { DirectoryFilters, ProfileItem } from "@/services/api/users/types";

export function ProfilesPage() {
  const { tenantId, contextId } = useSessionContext();
  const [filters, setFilters] = useState<DirectoryFilters & { active: string }>({
    search: "",
    active: "",
    page: 1,
    pageSize: 20,
    sortBy: "name",
    sortDirection: "asc",
  });
  const [search, setSearch] = useState("");
  const [active, setActive] = useState("");
  const [manual, setManual] = useState(false);
  const query = useQuery({
    queryKey: ["profiles", tenantId, contextId, filters],
    queryFn: ({ signal }) => accessProfilesApi.list(directoryQuery(filters), contextId, signal),
    placeholderData: keepPreviousData,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });
  if (manual && !query.isFetching) setManual(false);
  function change(next: typeof filters) {
    setManual(true);
    setFilters(next);
  }
  const columns: SelectableTableColumn<ProfileItem, string>[] = [
    { id: "code", header: "Código", render: (p) => p.code },
    { id: "name", header: "Nome", render: (p) => p.name },
    {
      id: "active",
      header: "Situação",
      render: (p) => (
        <StatusBadge
          label={p.active ? "Ativo" : "Inativo"}
          tone={p.active ? "success" : "default"}
        />
      ),
    },
    {
      id: "actions",
      header: "Ações",
      align: "right",
      sortable: false,
      render: (p) => (
        <Button component={Link} href={`/profiles/${p.id}`} size="small">
          Editar
        </Button>
      ),
    },
  ];
  return (
    <Stack spacing={3}>
      <LoadingState
        mode="blocking"
        open={query.isPending || (manual && query.isFetching)}
        message="Consultando perfis..."
      />
      <Stack
        direction="row"
        sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}
      >
        <FinancePageTitle navigationHref="/profiles">Perfis</FinancePageTitle>
        <Button component={Link} href="/profiles/new" variant="contained">
          Novo perfil
        </Button>
      </Stack>
      {query.isError && (
        <Alert
          severity="error"
          action={
            <Button
              onClick={() => {
                setManual(true);
                void query.refetch();
              }}
            >
              Tentar novamente
            </Button>
          }
        >
          Não foi possível consultar os perfis.
        </Alert>
      )}
      <Stack
        component="form"
        direction="row"
        sx={{ alignItems: "flex-end", columnGap: 1.25, flexWrap: "wrap", rowGap: 2 }}
        onSubmit={(event) => {
          event.preventDefault();
          const next = { ...filters, search, active, page: 1 };
          if (directoryQuery(next) === directoryQuery(filters)) {
            setManual(true);
            void query.refetch();
          } else change(next);
        }}
      >
        <TextField
          size="small"
          label="Nome ou código"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <TextField
          size="small"
          select
          slotProps={{ select: { native: true }, inputLabel: { shrink: true } }}
          label="Situação"
          value={active}
          onChange={(e) => setActive(e.target.value)}
          sx={{ minWidth: 150 }}
        >
          <option value="">Todos</option>
          <option value="true">Ativo</option>
          <option value="false">Inativo</option>
        </TextField>
        <Button variant="contained" type="submit">
          Filtrar
        </Button>
        <Button
          variant="outlined"
          type="button"
          onClick={() => {
            setSearch("");
            setActive("");
            const next = { ...filters, search: "", active: "", page: 1 };
            if (directoryQuery(next) === directoryQuery(filters)) {
              setManual(true);
              void query.refetch();
            } else change(next);
          }}
        >
          Limpar
        </Button>
      </Stack>
      <PrototypePanel title="Perfis da empresa">
        <SelectablePaginatedTable<ProfileItem, string>
          headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
          rows={query.data?.items ?? []}
          columns={columns}
          getRowId={(p) => p.id}
          showSelection={false}
          selectedRows={[]}
          onSelectionChange={() => {}}
          tableLabel="Perfis da empresa"
          emptyContent={query.isError ? "Consulta indisponível." : "Nenhum perfil encontrado."}
          sortBy={filters.sortBy}
          sortDirection={filters.sortDirection}
          onSort={(key) =>
            change({
              ...filters,
              page: 1,
              sortBy: key,
              sortDirection:
                filters.sortBy === key && filters.sortDirection === "asc" ? "desc" : "asc",
            })
          }
          pagination={{
            page: filters.page,
            pageSize: filters.pageSize,
            totalItems: query.data?.totalCount ?? 0,
            onPageChange: (page) => change({ ...filters, page }),
          }}
        />
      </PrototypePanel>
    </Stack>
  );
}
