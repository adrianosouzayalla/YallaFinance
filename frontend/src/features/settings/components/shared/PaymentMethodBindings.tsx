"use client";

import { useState } from "react";
import { Box, Button, FormControlLabel, MenuItem, Stack, Switch, TextField } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useToast } from "@/providers/ToastProvider";
import { usePaymentMethodMutations } from "../../hooks/useConnectionAdminMutations";
import type {
  CompanyConnectionDetail,
  CompanyConnectionDetailFilter,
  CompanyPaymentMethod,
} from "@/services/api/settings/types";
import {
  errorMessage,
  fieldGrid,
  ReadTable,
  useReportMutationBusy,
  type BusyReporter,
} from "./SettingsComponents";

const instrumentLabel = (value?: string | null) =>
  value === "Pix" ? "Pix" : value === "CreditCard" ? "Cartão de crédito" : value || "Não informada";
const methodLabel = (name?: string | null, description?: string | null) =>
  description
    ? `${name || "Forma sem nome cadastrada"} — ${description}`
    : name || "Forma sem nome cadastrada";

export function PaymentMethodBindings({
  id,
  detail,
  filters,
  onFilters,
  reportBusy,
  reportDirty,
}: {
  id: string | null;
  detail?: CompanyConnectionDetail;
  filters: CompanyConnectionDetailFilter;
  onFilters(filters: CompanyConnectionDetailFilter): void;
  reportBusy: BusyReporter;
  reportDirty(resource: string, dirty: boolean): void;
}) {
  const [editing, setEditing] = useState<CompanyPaymentMethod | null>(null);
  const [methodId, setMethodId] = useState("");
  const [instrument, setInstrument] = useState("");
  const [description, setDescription] = useState("");
  const [enabled, setEnabled] = useState(false);
  const mutations = usePaymentMethodMutations(id);
  const { showToast } = useToast();
  const resource = "payment-method-binding";
  useReportMutationBusy(
    resource,
    mutations.save.isPending || mutations.remove.isPending,
    reportBusy,
  );
  const supported = detail?.supportedPaymentInstruments ?? [];
  const options = detail?.availablePaymentMethods ?? [];
  const selected = options.find((option) => option.id === methodId);
  const conflicts = detail?.paymentMethodProviderConflicts ?? [];
  const available = options.filter(
    (option) =>
      !conflicts.some((conflict) => conflict.paymentMethodId === option.id) &&
      !detail?.paymentMethods.items.some((link) => link.paymentMethodId === option.id) &&
      (option.instrumentType ? supported.includes(option.instrumentType) : option.canClassify),
  );
  const instruments =
    instrument && !supported.includes(instrument) ? [...supported, instrument] : supported;
  const change = () => reportDirty(resource, true);

  function reset() {
    setEditing(null);
    setMethodId("");
    setInstrument("");
    setDescription("");
    setEnabled(false);
    reportDirty(resource, false);
  }

  function edit(row: CompanyPaymentMethod) {
    setEditing(row);
    setMethodId(row.paymentMethodId);
    setInstrument(row.instrumentType ?? "");
    setDescription(row.internalDescription ?? "");
    setEnabled(row.enabled);
    reportDirty(resource, true);
  }

  async function save() {
    if (!id || !methodId) return;
    try {
      await mutations.save.mutateAsync({
        linkId: editing?.id ?? null,
        draft: {
          paymentMethodId: methodId,
          enabled,
          instrumentType: instrument || null,
          internalDescription: description,
        },
      });
      reset();
      showToast({ severity: "success", message: "Vínculo de pagamento salvo." });
    } catch (error) {
      showToast({
        severity: "error",
        message: errorMessage(error, "Não foi possível salvar o vínculo de pagamento."),
      });
    }
  }

  async function remove(row: CompanyPaymentMethod) {
    if (!id || !window.confirm("Remover este vínculo de pagamento?")) return;
    try {
      await mutations.remove.mutateAsync(row.id);
      if (editing?.id === row.id) reset();
      showToast({ severity: "success", message: "Vínculo de pagamento removido." });
    } catch (error) {
      showToast({
        severity: "error",
        message: errorMessage(error, "Não foi possível remover o vínculo de pagamento."),
      });
    }
  }

  return (
    <Stack spacing={2.5}>
      <PrototypePanel
        title={editing ? "Editar vínculo de pagamento" : "Vincular forma de pagamento"}
      >
        <Box sx={{ ...fieldGrid, p: 2.5 }}>
          <TextField
            select
            label="Forma de pagamento"
            size="small"
            value={methodId}
            disabled={!detail || !!editing}
            helperText="Selecione uma forma cadastrada nesta empresa."
            onChange={(event) => {
              const option = options.find((item) => item.id === event.target.value);
              setMethodId(event.target.value);
              setInstrument(option?.instrumentType ?? "");
              setDescription(option?.internalDescription ?? "");
              change();
            }}
          >
            <MenuItem value="">Selecione</MenuItem>
            {editing ? (
              <MenuItem value={editing.paymentMethodId}>
                {methodLabel(editing.paymentMethodName, editing.internalDescription)}
              </MenuItem>
            ) : (
              available.map((option) => (
                <MenuItem key={option.id} value={option.id}>
                  {methodLabel(option.name, option.internalDescription)}
                </MenuItem>
              ))
            )}
          </TextField>
          <TextField
            select
            label="Modalidade"
            size="small"
            value={instrument}
            disabled={!detail || !!editing || !!selected?.instrumentType || !selected?.canClassify}
            helperText={
              editing && !instrument
                ? "Classificação de vínculo existente exige atualização administrativa."
                : "Modalidades implementadas por esta conexão."
            }
            onChange={(event) => {
              setInstrument(event.target.value);
              change();
            }}
          >
            <MenuItem value="">Selecione</MenuItem>
            {instruments.map((value) => (
              <MenuItem key={value} value={value}>
                {instrumentLabel(value)}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Descrição interna"
            size="small"
            value={description}
            disabled={!detail}
            helperText="Opcional. Identifica esta forma internamente; o nome apresentado ao cliente é preservado."
            onChange={(event) => {
              setDescription(event.target.value);
              change();
            }}
          />
          <FormControlLabel
            label="Vínculo habilitado"
            control={
              <Switch
                checked={enabled}
                disabled={!detail}
                onChange={(_, checked) => {
                  setEnabled(checked);
                  change();
                }}
              />
            }
          />
        </Box>
      </PrototypePanel>
      <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
        <Button variant="outlined" onClick={reset}>
          Cancelar vínculo
        </Button>
        <Button
          variant="contained"
          disabled={
            !id || !detail || !methodId || (!editing && !instrument) || mutations.save.isPending
          }
          onClick={() => void save()}
        >
          Salvar vínculo
        </Button>
      </Stack>
      <ReadTable
        title="Formas de pagamento vinculadas"
        page={detail?.paymentMethods}
        loaded={!!detail}
        columns={[
          {
            id: "paymentMethodName",
            header: "Forma de pagamento",
            render: (row) =>
              methodLabel(row.paymentMethodName, row.internalDescription) +
              (row.paymentMethodActive === false ? " (inativa)" : ""),
          },
          {
            id: "instrumentType",
            header: "Modalidade",
            sortable: false,
            render: (row) => instrumentLabel(row.instrumentType),
          },
          { id: "enabled", header: "Habilitado", render: (row) => (row.enabled ? "Sim" : "Não") },
          {
            id: "actions",
            header: "Ações",
            sortable: false,
            render: (row) => (
              <Stack direction="row">
                <Button onClick={() => edit(row)}>Editar</Button>
                <Button color="error" onClick={() => void remove(row)}>
                  Remover
                </Button>
              </Stack>
            ),
          },
        ]}
        sortBy={filters.paymentMethodSortBy}
        sortDirection={filters.paymentMethodSortDirection}
        onSort={(key) =>
          onFilters({
            ...filters,
            paymentMethodPage: 1,
            paymentMethodSortBy: key as CompanyConnectionDetailFilter["paymentMethodSortBy"],
            paymentMethodSortDirection:
              filters.paymentMethodSortBy === key && filters.paymentMethodSortDirection === "asc"
                ? "desc"
                : "asc",
          })
        }
        onPage={(page) => onFilters({ ...filters, paymentMethodPage: page })}
      />
    </Stack>
  );
}
