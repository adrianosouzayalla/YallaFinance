import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import Link from "next/link";
import { Button } from "@mui/material";
import type { ReactNode } from "react";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { DebtStatusBadge } from "@/features/debts/components/DebtStatusBadge";
import type { Debt, DebtSortKey } from "@/services/api/debts/debt.types";
import { financeTokens } from "@/theme/designTokens";

interface DebtTableProps {
  debts: Debt[];
  selectedDebts: Debt[];
  onSelectionChange(debts: Debt[]): void;
  sortBy?: DebtSortKey;
  sortDirection: "asc" | "desc";
  onSort(sortBy: DebtSortKey): void;
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange(page: number): void;
  loading?: boolean;
  selectionActions?: ReactNode;
}

// CODEX-DESIGN-LOCK: preserve the consultation column sequence and presentation.
const columns: SelectableTableColumn<Debt, DebtSortKey>[] = [
  { id: "category", header: "Categoria", sortKey: "category", render: (debt) => debt.category },
  { id: "customer", header: "Locatário", sortKey: "tenantName", render: (debt) => debt.tenantName },
  { id: "contract", header: "Contrato", sortKey: "contract", render: (debt) => debt.contract },
  { id: "plate", header: "Placa", sortKey: "plate", render: (debt) => debt.plate },
  {
    id: "date",
    header: "Data",
    sortKey: "occurrenceDate",
    render: (debt) => formatDate(debt.occurrenceDate),
  },
  {
    id: "amount",
    header: "Valor",
    sortKey: "amount",
    align: "right",
    render: (debt) => formatCurrency(debt.amount),
  },
  {
    id: "status",
    header: "Status",
    sortKey: "status",
    render: (debt) => <DebtStatusBadge status={debt.status} />,
  },
  {
    id: "action",
    sortable: false,
    header: "Ação",
    align: "right",
    render: (debt) => (
      <Button component={Link} href={`/debts/${debt.id}`} size="small">
        Ver
      </Button>
    ),
  },
];

/** Debt-specific columns and eligibility; selection and pagination use the shared table. */
export function DebtTable({
  debts,
  selectedDebts,
  onSelectionChange,
  sortBy,
  sortDirection,
  onSort,
  page,
  pageSize,
  totalItems,
  onPageChange,
  loading,
  selectionActions,
}: DebtTableProps) {
  return (
    <SelectablePaginatedTable
      rows={debts}
      columns={columns}
      getRowId={(debt) => debt.id}
      isRowSelectable={(debt) => debt.status === "Open"}
      getRowSelectionLabel={(debt) => `Selecionar débito ${debt.category} de ${debt.tenantName}`}
      tableLabel="Lista de débitos"
      selectPageLabel="Selecionar todos os débitos"
      headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
      selectedRows={selectedDebts}
      onSelectionChange={onSelectionChange}
      sortBy={sortBy}
      sortDirection={sortDirection}
      onSort={onSort}
      loading={loading}
      pagination={{ page, pageSize, totalItems, onPageChange, label: "Paginação de débitos" }}
      emptyContent="Nenhum débito encontrado para os filtros aplicados"
      selectionActions={selectionActions}
    />
  );
}
