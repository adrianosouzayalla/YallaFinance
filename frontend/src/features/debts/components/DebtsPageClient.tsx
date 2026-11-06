"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";

import { formatCurrency } from "@/utils/currency";
import { useDebtSearch } from "@/features/debts/hooks/useDebtSearch";
import { useCategories } from "@/features/catalogs/hooks/useCategories";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { Alert, Box, Button, Stack } from "@mui/material";
import { usePaginatedSelection } from "@/hooks/usePaginatedSelection";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { DebtFilters } from "@/features/debts/components/DebtFilters";
import { DebtTable } from "@/features/debts/components/DebtTable";
import { LoadingState } from "@/components/ui/LoadingState";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { SelectionBar } from "@/features/debts/components/SelectionBar";
import { SummaryCard } from "@/components/ui/SummaryCard";
import { validateDebtSelection } from "@/features/charges/businessRules";
import { debtStatusFromQuery } from "@/features/debts/debtStatus";
import { buildReviewUrl } from "@/features/debts/debtNavigation";
import type {
  Debt,
  DebtFilters as DebtFiltersValue,
  DebtSortKey,
} from "@/services/api/debts/debt.types";
import { useToast } from "@/providers/ToastProvider";

const defaultFilters: DebtFiltersValue = {
  categories: [],
  status: "Todos",
  startDate: "",
  endDate: "",
  sortDirection: "desc",
  page: 1,
  pageSize: 5,
};

function filtersFromDashboardUrl(): DebtFiltersValue | null {
  if (typeof window === "undefined") {
    return null;
  }

  const params = new URLSearchParams(window.location.search);
  const statusParam = params.get("status");
  const sortParam = params.get("sort");
  const status = debtStatusFromQuery(statusParam);

  if (status === undefined && sortParam === null) {
    return null;
  }

  const orderedByDashboard = sortParam === "overdue" || sortParam === "stale";
  const dashboardEndDate = "";

  return {
    ...defaultFilters,
    endDate: dashboardEndDate,
    page: 1,
    sortBy: orderedByDashboard ? "occurrenceDate" : undefined,
    sortDirection: orderedByDashboard ? "asc" : defaultFilters.sortDirection,
    startDate: "",
    status: status ?? "Todos",
  };
}

export function DebtsPageClient() {
  const debtQuery = useDebtSearch();
  const load = debtQuery.load;
  const categoryQuery = useCategories();
  const result = debtQuery.isError ? null : (debtQuery.data ?? null);
  const error = debtQuery.isError;
  const categories = categoryQuery.data ?? [];
  const { showToast } = useToast();
  const [draftFilters, setDraftFilters] = useState<DebtFiltersValue>(defaultFilters);
  const [appliedFilters, setAppliedFilters] = useState<DebtFiltersValue | null>(null);

  const [invalidUrlStatus, setInvalidUrlStatus] = useState(false);
  const loading = debtQuery.isFetching || (!debtQuery.started && !invalidUrlStatus);
  const { tenantId, contextId } = useSessionContext();
  const {
    selectedRows: selectedDebts,
    setSelectedRows,
    clearSelection,
  } = usePaginatedSelection<Debt>({
    scopeKey: JSON.stringify([tenantId, contextId]),
  });

  const requestInFlightRef = useRef(false);

  const loadDebts = useCallback(
    async (filters: DebtFiltersValue | null): Promise<boolean> => {
      if (requestInFlightRef.current) {
        return false;
      }

      requestInFlightRef.current = true;

      setInvalidUrlStatus(false);
      try {
        const listFilters = filters ?? {
          ...defaultFilters,
          status: "Todos" as const,
          startDate: "",
          endDate: "",
          sortBy: undefined,
        };
        await load(listFilters);
        return true;
      } catch {
        showToast({
          message: "Não foi possível carregar os débitos. Tente novamente.",
          severity: "error",
        });
        return false;
      } finally {
        requestInFlightRef.current = false;
      }
    },
    [load, showToast],
  );

  useEffect(() => {
    void Promise.resolve().then(() => {
      let initialFilters: DebtFiltersValue | null;
      try {
        initialFilters = filtersFromDashboardUrl();
        const storageKey = `debt-filters:${JSON.stringify([tenantId, contextId])}`;
        const stored = sessionStorage.getItem(storageKey);
        if (stored && initialFilters === null) {
          const saved = JSON.parse(stored) as DebtFiltersValue;
          if (
            Array.isArray(saved.categories) &&
            saved.categories.every((value) => typeof value === "string") &&
            (saved.status === "Todos" || debtStatusFromQuery(saved.status) !== undefined) &&
            typeof saved.startDate === "string" &&
            typeof saved.endDate === "string" &&
            Number.isInteger(saved.page) &&
            saved.page > 0 &&
            Number.isInteger(saved.pageSize) &&
            saved.pageSize > 0 &&
            (saved.sortDirection === "asc" || saved.sortDirection === "desc")
          )
            initialFilters = saved;
        }
        sessionStorage.removeItem(storageKey);
      } catch {
        setInvalidUrlStatus(true);
        return;
      }
      setDraftFilters(initialFilters ?? defaultFilters);
      setAppliedFilters(initialFilters);
      return loadDebts(initialFilters);
    });
  }, [loadDebts, tenantId, contextId]);

  useEffect(() => {
    if (categoryQuery.isError)
      showToast({ message: "Não foi possível carregar as categorias.", severity: "error" });
  }, [categoryQuery.isError, showToast]);

  const selection = validateDebtSelection(selectedDebts);
  const reviewUrl =
    selectedDebts.length > 0 && selection.valid ? buildReviewUrl(selectedDebts) : undefined;

  function applyFilters(
    nextFilters = draftFilters,
    successMessage?: string,
    preserveSelection = false,
  ) {
    if (requestInFlightRef.current) {
      return;
    }

    const normalized = { ...nextFilters, page: Math.max(1, nextFilters.page) };
    const url = new URL(window.location.href);
    if (normalized.status === "Todos") url.searchParams.delete("status");
    else url.searchParams.set("status", normalized.status);
    window.history.replaceState(null, "", url);
    setDraftFilters(normalized);
    setAppliedFilters(normalized);
    if (!preserveSelection) clearSelection();
    void loadDebts(normalized).then((loaded) => {
      if (loaded && successMessage !== undefined) {
        showToast({ message: successMessage, severity: "success" });
      }
    });
  }

  function clearFilters() {
    if (requestInFlightRef.current) {
      return;
    }

    setDraftFilters(defaultFilters);
    const url = new URL(window.location.href);
    url.searchParams.delete("status");
    url.searchParams.delete("sort");
    window.history.replaceState(null, "", url);
    setAppliedFilters(null);
    clearSelection();
    void loadDebts(null).then((loaded) => {
      if (loaded) {
        showToast({ message: "Filtros limpos.", severity: "success" });
      }
    });
  }

  function sortBy(sortKey: DebtSortKey) {
    const current = appliedFilters ?? defaultFilters;
    const nextFilters = {
      ...current,
      page: 1,
      sortBy: sortKey,
      sortDirection:
        current.sortBy === sortKey && current.sortDirection === "asc"
          ? ("desc" as const)
          : ("asc" as const),
    };
    applyFilters(nextFilters, undefined, true);
  }

  function changePage(page: number) {
    applyFilters({ ...(appliedFilters ?? defaultFilters), page }, undefined, true);
  }

  return (
    <>
      <LoadingState mode="blocking" open={loading} message="Consultando débitos..." />
      <Stack spacing={3}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
          <Box>
            <FinancePageTitle navigationHref="/debts">Débitos</FinancePageTitle>
          </Box>
          <Stack direction="row" spacing={1.5}>
            <Button
              component={Link}
              href="/debts/new"
              variant="contained"
              onClick={() => {
                sessionStorage.setItem(
                  `debt-filters:${JSON.stringify([tenantId, contextId])}`,
                  JSON.stringify(appliedFilters ?? draftFilters),
                );
              }}
            >
              + Débito avulso
            </Button>
            <Button component={Link} href="/charges/new" variant="outlined">
              + Nova cobrança avulsa
            </Button>
          </Stack>
        </Stack>

        <DebtFilters
          categories={categories}
          disabled={loading}
          value={draftFilters}
          onChange={setDraftFilters}
          onClear={clearFilters}
          onSubmit={() => applyFilters(draftFilters, "Filtros aplicados.")}
        />

        <Box
          sx={{
            // CODEX-DESIGN-LOCK: the consultation wireframe fixes four equal summary cards in this order.
            display: "grid",
            gap: 2,
            gridTemplateColumns: "repeat(4, minmax(0, 1fr))",
          }}
        >
          <SummaryCard
            label="Débitos em aberto"
            value={result ? String(result.summary.openCount) : "—"}
          />
          <SummaryCard
            label="Valor em aberto"
            value={result ? formatCurrency(result.summary.openAmount) : "—"}
          />
          <SummaryCard
            label="Em cobrança"
            value={result ? formatCurrency(result.summary.chargingAmount) : "—"}
          />
          <SummaryCard
            label="Quitados no período"
            value={result ? formatCurrency(result.summary.paidAmount) : "—"}
          />
        </Box>

        {invalidUrlStatus ? (
          <Alert severity="error">
            Status da URL inválido. Escolha um status no filtro ou clique em Limpar.
          </Alert>
        ) : error ? (
          <Alert
            severity="error"
            action={
              <Button onClick={() => void loadDebts(appliedFilters)}>Tentar novamente</Button>
            }
          >
            Não foi possível carregar os dados do Yalla Digital. Tente novamente.
          </Alert>
        ) : (
          <PrototypePanel title="Débitos">
            <DebtTable
              debts={result?.items ?? []}
              selectedDebts={selectedDebts}
              onSelectionChange={setSelectedRows}
              sortBy={appliedFilters?.sortBy}
              sortDirection={appliedFilters?.sortDirection ?? "desc"}
              onSort={sortBy}
              page={result?.page ?? 1}
              pageSize={result?.pageSize ?? defaultFilters.pageSize}
              totalItems={result?.totalItems ?? 0}
              onPageChange={changePage}
              loading={loading}
              selectionActions={
                <SelectionBar
                  count={selectedDebts.length}
                  reviewUrl={reviewUrl}
                  tenantName={selectedDebts[0]?.tenantName}
                  total={formatCurrency(
                    selectedDebts.reduce((total, debt) => total + debt.amount, 0),
                  )}
                  validationMessage={selection.valid ? undefined : selection.message}
                  onClear={clearSelection}
                />
              }
            />
          </PrototypePanel>
        )}
      </Stack>
    </>
  );
}
