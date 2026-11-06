"use client";

import { useEffect } from "react";
import { MenuItem, TextField } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";
import type { SettingsField } from "../../settingsFields";
import type { CompanyPage } from "@/services/api/settings/types";

export type BusyReporter = (resource: string, busy: boolean) => void;

export const fieldGrid = {
  display: "grid",
  gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "repeat(2, minmax(0, 1fr))" },
  gap: 2,
};

export const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleString("pt-BR") : "—";
export const groupLabel = (value: string) =>
  value === "FinancialSystem"
    ? "Sistema financeiro"
    : value === "PaymentProvider"
      ? "Provider de pagamento"
      : value;

export function useQueryFeedback(
  resource: string,
  query: { isFetching: boolean; isError: boolean; errorUpdatedAt: number },
  reportBusy: BusyReporter,
  message: string,
) {
  const { showToast } = useToast();
  useEffect(() => {
    reportBusy(resource, query.isFetching);
    return () => reportBusy(resource, false);
  }, [resource, query.isFetching, reportBusy]);
  useEffect(() => {
    if (query.isError) showToast({ severity: "error", message });
  }, [query.isError, query.errorUpdatedAt, message, showToast]);
}

export function useReportMutationBusy(resource: string, busy: boolean, reportBusy: BusyReporter) {
  useEffect(() => {
    reportBusy(resource, busy);
    return () => reportBusy(resource, false);
  }, [resource, busy, reportBusy]);
}

export function errorMessage(error: unknown, fallback: string) {
  const errorRecord =
    error && typeof error === "object" ? (error as Record<string, unknown>) : undefined;
  const directMessage = error instanceof Error ? error.message : errorRecord?.message;
  if (typeof directMessage === "string" && directMessage.trim()) return directMessage;
  if (typeof errorRecord?.detail === "string" && errorRecord.detail.trim())
    return errorRecord.detail;

  const response =
    errorRecord?.response && typeof errorRecord.response === "object"
      ? (errorRecord.response as Record<string, unknown>)
      : undefined;
  let responseData = response?.data;
  if (typeof responseData === "string") {
    try {
      responseData = JSON.parse(responseData);
    } catch {
      responseData = undefined;
    }
  }
  if (responseData && typeof responseData === "object") {
    const problem = responseData as Record<string, unknown>;
    if (typeof problem.detail === "string" && problem.detail.trim()) return problem.detail;
    if (Array.isArray(problem.errors)) {
      const firstError = problem.errors.find((item) => item && typeof item === "object") as
        Record<string, unknown> | undefined;
      if (typeof firstError?.message === "string" && firstError.message.trim())
        return firstError.message;
    }
  }
  return fallback;
}

export function SettingsInput({
  field,
  persisted,
  value,
  onChange,
  disabled = false,
  validateRequired = false,
}: {
  field: SettingsField;
  persisted?: string | null;
  value?: string;
  onChange?(value: string): void;
  disabled?: boolean;
  validateRequired?: boolean;
}) {
  const currentValue = value ?? persisted ?? "";
  const invalidNumber =
    field.kind === "number" &&
    currentValue !== "" &&
    (!/^\d+$/.test(currentValue) ||
      !Number.isSafeInteger(Number(currentValue)) ||
      (field.min !== undefined && Number(currentValue) < field.min) ||
      (field.max !== undefined && Number(currentValue) > field.max));
  const invalidRequired = validateRequired && field.required && currentValue.trim() === "";
  const options = field.options ?? [];
  return (
    <TextField
      id={`setting-${field.id}`}
      name={field.id}
      label={field.label}
      value={currentValue}
      onChange={(event) => onChange?.(event.target.value)}
      disabled={disabled}
      required={field.required}
      fullWidth
      size="small"
      error={invalidNumber || invalidRequired}
      select={field.kind === "select"}
      type={field.kind === "number" && !invalidNumber ? "number" : "text"}
      multiline={field.kind === "multiline"}
      minRows={field.kind === "multiline" ? 4 : undefined}
      helperText={
        invalidRequired
          ? "Campo obrigatório."
          : invalidNumber
            ? "Valor inválido. Confira o formato e os limites deste campo."
            : field.helper
      }
      autoComplete="off"
      slotProps={{
        input: { readOnly: field.readOnly },
        htmlInput: {
          min: field.min,
          max: field.max,
          maxLength: field.maxLength,
          autoCapitalize: "none",
          spellCheck: false,
        },
      }}
    >
      {field.kind === "select"
        ? [
            <MenuItem key="empty" value="">
              Selecione
            </MenuItem>,
            ...options.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            )),
            ...(currentValue && !options.some((option) => option.value === currentValue)
              ? [
                  <MenuItem key={`unsupported-${currentValue}`} value={currentValue}>
                    {currentValue} (não suportado)
                  </MenuItem>,
                ]
              : []),
          ]
        : null}
    </TextField>
  );
}

export function ReadTable<T>({
  title,
  page,
  columns,
  onPage,
  sortBy,
  sortDirection,
  onSort,
  loaded = true,
}: {
  title: string;
  page?: CompanyPage<T>;
  columns: SelectableTableColumn<T>[];
  onPage(page: number): void;
  sortBy: string;
  sortDirection: "asc" | "desc";
  onSort(key: string): void;
  loaded?: boolean;
}) {
  return (
    <PrototypePanel title={title}>
      <SelectablePaginatedTable<T>
        rows={page?.items ?? []}
        columns={columns}
        getRowId={(row) => (row as { id: string }).id}
        selectedRows={[]}
        onSelectionChange={() => {}}
        showSelection={false}
        tableLabel={title}
        headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
        sortBy={sortBy}
        sortDirection={sortDirection}
        onSort={onSort}
        pagination={{
          page: page?.page ?? 1,
          pageSize: page?.pageSize ?? 20,
          totalItems: page?.totalItems ?? 0,
          onPageChange: onPage,
          label: `Paginação de ${title}`,
        }}
        emptyContent={loaded ? "Nenhum registro encontrado." : "Consulta ainda não disponível."}
      />
    </PrototypePanel>
  );
}
