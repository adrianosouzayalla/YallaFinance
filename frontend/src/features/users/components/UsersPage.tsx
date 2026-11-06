"use client";
import { useState } from "react";
import Link from "next/link";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Alert, Button, Stack, TextField } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { LoadingState } from "@/components/ui/LoadingState";
import { StatusBadge } from "@/components/ui/StatusBadge";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { directoryQuery } from "@/services/api/directoryApi";
import { usersApi } from "@/services/api/users/usersApi.client";
import type { DirectoryFilters, UserItem } from "@/services/api/users/types";

export function UsersPage() {
  const { tenantId, contextId } = useSessionContext();
  const [filters, setFilters] = useState<
    DirectoryFilters & { userActive: string; accessActive: string }
  >({
    search: "",
    userActive: "",
    accessActive: "",
    page: 1,
    pageSize: 20,
    sortBy: "name",
    sortDirection: "asc",
  });
  const [search, setSearch] = useState("");
  const [userActive, setUserActive] = useState("");
  const [accessActive, setAccessActive] = useState("");
  const [manual, setManual] = useState(false);
  const query = useQuery({
    queryKey: ["users", tenantId, contextId, filters],
    queryFn: ({ signal }) => usersApi.list(directoryQuery(filters), contextId, signal),
    placeholderData: keepPreviousData,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });
  if (manual && !query.isFetching) setManual(false);
  const columns: SelectableTableColumn<UserItem, string>[] = [
    { id: "name", header: "Nome", render: (u) => u.name },
    { id: "login", header: "Login", render: (u) => u.login },
    { id: "email", header: "E-mail", render: (u) => u.email || "—" },
    {
      id: "active",
      header: "Usuário",
      render: (u) => (
        <StatusBadge
          label={u.active ? "Ativo" : "Inativo"}
          tone={u.active ? "success" : "default"}
        />
      ),
    },
    {
      id: "accessActive",
      header: "Vínculo",
      render: (u) => (
        <StatusBadge
          label={u.accessActive ? "Ativo" : "Inativo"}
          tone={u.accessActive ? "success" : "default"}
        />
      ),
    },
    {
      id: "profile",
      header: "Perfil",
      sortable: false,
      render: (u) => u.profileName || "Sem perfil",
    },
    {
      id: "actions",
      header: "Ações",
      align: "right",
      sortable: false,
      render: (u) => (
        <Button component={Link} href={`/users/${u.id}`} size="small">
          Editar
        </Button>
      ),
    },
  ];
  const changeFilters = (next: typeof filters) => {
    setManual(true);
    setFilters(next);
  };
  return (
    <Stack spacing={3}>
      <LoadingState
        mode="blocking"
        open={query.isPending || (manual && query.isFetching)}
        message="Consultando usuários..."
      />
      <Stack
        direction="row"
        sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}
      >
        <FinancePageTitle navigationHref="/users">Usuários</FinancePageTitle>
        <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: "wrap" }}>
          <Button component={Link} href="/users/new" variant="contained">
            Novo usuário
          </Button>
        </Stack>
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
          Não foi possível consultar os usuários.
        </Alert>
      )}
      <Stack
        component="form"
        direction="row"
        sx={{ alignItems: "flex-end", columnGap: 1.25, flexWrap: "wrap", rowGap: 2 }}
        onSubmit={(event) => {
          event.preventDefault();
          const next = { ...filters, search, userActive, accessActive, page: 1 };
          if (directoryQuery(next) === directoryQuery(filters)) {
            setManual(true);
            void query.refetch();
          } else changeFilters(next);
        }}
      >
        <TextField
          size="small"
          label="Nome, login ou e-mail"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <TextField
          size="small"
          select
          slotProps={{ select: { native: true }, inputLabel: { shrink: true } }}
          label="Situação do usuário"
          value={userActive}
          onChange={(e) => setUserActive(e.target.value)}
          sx={{ minWidth: 180 }}
        >
          <option value="">Todos</option>
          <option value="true">Ativo</option>
          <option value="false">Inativo</option>
        </TextField>
        <TextField
          size="small"
          select
          slotProps={{ select: { native: true }, inputLabel: { shrink: true } }}
          label="Situação do vínculo"
          value={accessActive}
          onChange={(e) => setAccessActive(e.target.value)}
          sx={{ minWidth: 180 }}
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
            setUserActive("");
            setAccessActive("");
            const next = { ...filters, search: "", userActive: "", accessActive: "", page: 1 };
            if (directoryQuery(next) === directoryQuery(filters)) {
              setManual(true);
              void query.refetch();
            } else changeFilters(next);
          }}
        >
          Limpar
        </Button>
      </Stack>
      <PrototypePanel title="Usuários da empresa">
        <SelectablePaginatedTable<UserItem, string>
          rows={query.data?.items ?? []}
          columns={columns}
          showSelection={false}
          headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
          selectedRows={[]}
          onSelectionChange={() => {}}
          getRowId={(u) => u.id}
          tableLabel="Usuários da empresa"
          emptyContent={query.isError ? "Consulta indisponível." : "Nenhum usuário encontrado."}
          sortBy={filters.sortBy}
          sortDirection={filters.sortDirection}
          onSort={(key) =>
            changeFilters({
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
            onPageChange: (page) => changeFilters({ ...filters, page }),
          }}
        />
      </PrototypePanel>
    </Stack>
  );
}
