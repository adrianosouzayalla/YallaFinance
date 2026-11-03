"use client";

import { useState, type ReactNode } from "react";
import {
  Checkbox,
  Pagination,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  type TableCellProps,
  type SxProps,
  type Theme,
} from "@mui/material";

export interface SelectableTableColumn<T, SortKey extends string = string> {
  id: string;
  header: ReactNode;
  render(row: T): ReactNode;
  align?: TableCellProps["align"];
  sortKey?: SortKey;
  sortable?: boolean;
  getSortValue?(row: T): string | number | boolean | Date | null | undefined;
}

export interface SelectablePaginatedTableProps<T, SortKey extends string = string> {
  rows: readonly T[];
  paginationMode?: "server" | "client";
  showSelection?: boolean;
  columns: readonly SelectableTableColumn<T, SortKey>[];
  getRowId(row: T): string | number;
  selectedRows: readonly T[];
  onSelectionChange(rows: T[]): void;
  isRowSelectable?(row: T): boolean;
  getRowSelectionLabel?(row: T): string;
  tableLabel?: string;
  selectPageLabel?: string;
  headerSx?: SxProps<Theme>;
  loading?: boolean;
  onRowClick?(row: T): void;
  getRowClickLabel?(row: T): string;
  sortBy?: SortKey;
  sortDirection?: "asc" | "desc";
  onSort?(key: SortKey): void;
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    onPageChange(page: number): void;
    label?: string;
  };
  emptyContent?: ReactNode;
  selectionActions?: ReactNode;
}

/** Shared header sorting; fetching and business actions remain with the caller. */
export function SelectablePaginatedTable<T, SortKey extends string = string>({
  rows,
  paginationMode = "server",
  showSelection = true,
  columns,
  getRowId,
  selectedRows,
  onSelectionChange,
  isRowSelectable = () => true,
  getRowSelectionLabel = (row) => `Selecionar registro ${getRowId(row)}`,
  tableLabel = "Lista de registros",
  selectPageLabel = "Selecionar todos os registros desta página",
  headerSx = { backgroundColor: "action.hover" },
  loading = false,
  onRowClick,
  getRowClickLabel,
  sortBy,
  sortDirection = "asc",
  onSort,
  pagination,
  emptyContent = "Nenhum registro encontrado",
  selectionActions,
}: SelectablePaginatedTableProps<T, SortKey>) {
  const [localSort, setLocalSort] = useState<{ key: string; direction: "asc" | "desc" }>();
  const currentKey = onSort ? sortBy : localSort?.key;
  const currentDirection = onSort ? sortDirection : localSort?.direction ?? "asc";
  const sortColumn = columns.find((column) => (column.sortKey ?? column.id) === currentKey);
  const orderedRows = !onSort && sortColumn ? [...rows].sort((left, right) => {
    const value = (row: T) => sortColumn.getSortValue ? sortColumn.getSortValue(row)
      : (row as Record<string, unknown>)[sortColumn.id] ?? sortColumn.render(row);
    const a = value(left); const b = value(right);
    if (a == null) return b == null ? 0 : 1;
    if (b == null) return -1;
    const result = a instanceof Date && b instanceof Date ? a.getTime() - b.getTime()
      : typeof a === "number" && typeof b === "number" ? a - b
      : typeof a === "boolean" && typeof b === "boolean" ? Number(a) - Number(b)
      : String(a).localeCompare(String(b), "pt-BR", { numeric: true, sensitivity: "base" });
    return currentDirection === "asc" ? result : -result;
  }) : rows;
  const visibleRows = paginationMode === "client"
    ? orderedRows.slice((pagination.page - 1) * pagination.pageSize, pagination.page * pagination.pageSize) : orderedRows;
  function changeSort(column: SelectableTableColumn<T, SortKey>) {
    const key = column.sortKey ?? column.id;
    if (onSort) { onSort(key as SortKey); return; }
    setLocalSort({ key, direction: currentKey === key && currentDirection === "asc" ? "desc" : "asc" });
    if (paginationMode === "client") pagination.onPageChange(1);
  }
  const selectedIds = new Set(selectedRows.map(getRowId));
  const selectableRows = visibleRows.filter(isRowSelectable);
  const pageIds = new Set(selectableRows.map(getRowId));
  const selectedPageCount = selectableRows.filter((row) => selectedIds.has(getRowId(row))).length;
  const allSelected = selectableRows.length > 0 && selectedPageCount === selectableRows.length;

  function toggleRow(row: T) {
    if (loading || !isRowSelectable(row)) return;
    const id = getRowId(row);
    onSelectionChange(
      selectedIds.has(id)
        ? selectedRows.filter((selected) => getRowId(selected) !== id)
        : [...selectedRows, row],
    );
  }

  function togglePage() {
    if (loading || selectableRows.length === 0) return;
    if (allSelected) {
      onSelectionChange(selectedRows.filter((row) => !pageIds.has(getRowId(row))));
    } else {
      const merged = new Map(selectedRows.map((row) => [getRowId(row), row]));
      for (const row of selectableRows) merged.set(getRowId(row), row);
      onSelectionChange([...merged.values()]);
    }
  }

  return (
    <>
      <TableContainer>
        <Table aria-label={tableLabel} aria-busy={loading} size="small">
          <TableHead>
            <TableRow sx={headerSx}>
              {showSelection && (
                <TableCell padding="checkbox" sx={{ width: 42 }}>
                  <Checkbox
                    size="small"
                    checked={allSelected}
                    disabled={loading || selectableRows.length === 0}
                    indeterminate={selectedPageCount > 0 && !allSelected}
                    slotProps={{ input: { "aria-label": selectPageLabel } }}
                    onChange={togglePage}
                  />
                </TableCell>
              )}
              {columns.map((column) => {
                const active = currentKey === (column.sortKey ?? column.id);
                return (
                  <TableCell
                    key={column.id}
                    align={column.align}
                    sortDirection={active ? currentDirection : false}
                  >
                    {column.sortable !== false ? (
                      <TableSortLabel
                        active={active}
                        direction={active ? currentDirection : "asc"}
                        disabled={loading}
                        onClick={() => changeSort(column)}
                      >
                        {column.header}
                      </TableSortLabel>
                    ) : (
                      column.header
                    )}
                  </TableCell>
                );
              })}
            </TableRow>
          </TableHead>
          <TableBody>
            {visibleRows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length + (showSelection ? 1 : 0)}>
                  {emptyContent}
                </TableCell>
              </TableRow>
            ) : (
              visibleRows.map((row) => (
                <TableRow
                  hover
                  key={getRowId(row)}
                  selected={selectedIds.has(getRowId(row))}
                  onClick={onRowClick ? (event) => {
                    if (loading) return;
                    if (event.target instanceof Element && event.target.closest("button, a, input, label, [role='button'], [role='checkbox'], [role='radio'], [role='switch']")) return;
                    onRowClick(row);
                  } : undefined}
                  onKeyDown={onRowClick ? (event) => {
                    if (loading || event.target !== event.currentTarget || (event.key !== "Enter" && event.key !== " ")) return;
                    event.preventDefault(); onRowClick(row);
                  } : undefined}
                  tabIndex={onRowClick ? 0 : undefined}
                  aria-label={getRowClickLabel?.(row)}
                  aria-disabled={onRowClick ? loading : undefined}
                  sx={onRowClick && !loading ? { cursor: "pointer" } : undefined}
                >
                  {showSelection && (
                    <TableCell padding="checkbox">
                      <Checkbox
                        size="small"
                        checked={selectedIds.has(getRowId(row))}
                        disabled={loading || !isRowSelectable(row)}
                        slotProps={{ input: { "aria-label": getRowSelectionLabel(row) } }}
                        onChange={() => toggleRow(row)}
                      />
                    </TableCell>
                  )}
                  {columns.map((column) => (
                    <TableCell key={column.id} align={column.align}>
                      {column.render(row)}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <Stack direction="row" sx={{ justifyContent: "flex-end", p: 1.5 }}>
        <Pagination
          count={Math.max(1, Math.ceil(pagination.totalItems / pagination.pageSize))}
          disabled={loading}
          page={pagination.page}
          shape="rounded"
          aria-label={pagination.label ?? "Paginação de registros"}
          onChange={(_, page) => pagination.onPageChange(page)}
        />
      </Stack>
      {selectionActions}
    </>
  );
}
