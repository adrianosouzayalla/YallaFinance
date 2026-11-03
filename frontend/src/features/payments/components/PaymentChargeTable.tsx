import { Button } from "@mui/material";
import { SelectablePaginatedTable, type SelectableTableColumn } from "@/components/ui/SelectablePaginatedTable";
import { financeTokens } from "@/theme/designTokens";
import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import { PaymentStatusBadge } from "./PaymentStatusBadge";
import { PaymentSelectionBar } from "./PaymentSelectionBar";
import { paymentDateOnly } from "../paymentDate";
import type { PaymentCharge, PaymentChargeList } from "../types/payment.types";

const columns: SelectableTableColumn<PaymentCharge>[] = [
  { id: "charge", header: "Cobrança", render: (row) => row.reference },
  { id: "customer", header: "Locatário", render: (row) => row.customerName },
  { id: "contract", header: "Contrato", render: (row) => row.contract },
  { id: "created", header: "Data", render: (row) => formatDate(paymentDateOnly(row.createdAt)) },
  { id: "due", header: "Vencimento", render: (row) => formatDate(row.dueDate) },
  { id: "amount", header: "Valor", align: "right", render: (row) => formatCurrency(row.amount) },
  { id: "eligibility", header: "Status do pagamento", sortKey: "eligibility", render: (row) => <PaymentStatusBadge charge={row} /> },
];

export function PaymentChargeTable({ data, selected, onSelect, onClear, onPage, loading, sortBy, sortDirection, onSort, onRowClick }: {
  sortBy?: string; sortDirection?: "asc" | "desc"; onSort(key: string): void;
  onRowClick?(row: PaymentCharge): void;
  data: PaymentChargeList; selected: PaymentCharge[]; onSelect(rows: PaymentCharge[]): void; onClear(): void; onPage(page: number): void; loading: boolean;
}) {
  const tableColumns: SelectableTableColumn<PaymentCharge>[] = onRowClick
    ? [...columns, { id: "actions", header: "Ações", sortable: false, render: (row) =>
      <Button size="small" disabled={loading} onClick={() => onRowClick(row)}>Detalhes</Button> }]
    : columns;
  return <SelectablePaginatedTable rows={data.items} columns={tableColumns} getRowId={(row) => row.id}
    onRowClick={onRowClick} getRowClickLabel={(row) => `Ver detalhes da cobrança ${row.id}`}
    sortBy={sortBy} sortDirection={sortDirection} onSort={onSort}
    selectedRows={selected} onSelectionChange={onSelect} isRowSelectable={(row) => row.eligible}
    getRowSelectionLabel={(row) => `Selecionar cobrança ${row.id}`} tableLabel="Cobranças sem recebimento"
    selectPageLabel="Selecionar cobranças desta página" headerSx={{ backgroundColor: financeTokens.colors.tableHeader }} loading={loading}
    pagination={{ page: data.page, pageSize: data.pageSize, totalItems: data.totalItems, onPageChange: onPage, label: "Paginação de cobranças" }}
    emptyContent="Nenhuma cobrança encontrada para os filtros informados"
    selectionActions={<PaymentSelectionBar rows={selected} onClear={onClear} />} />;
}
