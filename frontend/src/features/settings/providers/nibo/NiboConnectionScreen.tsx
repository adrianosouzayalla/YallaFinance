"use client";

import { useState } from "react";
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, Tab, Tabs, TextField, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useToast } from "@/providers/ToastProvider";
import { ConnectionAdminFields } from "../../components/shared/ConnectionAdminFields";
import { ProviderSettingsForm } from "../../components/shared/ProviderSettingsForm";
import { dateText, errorMessage, fieldGrid, ReadTable, useQueryFeedback, useReportMutationBusy, type BusyReporter } from "../../components/shared/SettingsComponents";
import { useCompanyCatalog, useCompanyConnection } from "../../hooks/useCompanyConnection";
import type { CompanyConnectionDetailFilter, CompanyMapping, CompanyMappingDraft } from "../../types";
import { niboSections } from "./settingsFields";
import { useNiboConnectionMutations } from "./useNiboConnectionMutations";
import type { SelectableTableColumn } from "@/components/ui/SelectablePaginatedTable";

type NiboDetail = Pick<NonNullable<ReturnType<typeof useCompanyConnection>["data"]>,
  "connection" | "settings" | "categoryMappings" | "receiptAccountMappings">;
type NiboFilters = Pick<CompanyConnectionDetailFilter,
  "categoryPage" | "receiptAccountPage" | "pageSize" | "categorySortBy" | "categorySortDirection"
  | "receiptAccountSortBy" | "receiptAccountSortDirection">;

const defaultFilters: NiboFilters = {
  categoryPage: 1,
  receiptAccountPage: 1,
  pageSize: 20,
  categorySortBy: "internalName",
  categorySortDirection: "asc",
  receiptAccountSortBy: "internalName",
  receiptAccountSortDirection: "asc",
};

function MappingInputs({ account, options, current, selectedId, externalValue, onSelectedId, onExternalValue,
  onSave, onCancel, disabled, saving }: {
  account: boolean; options: { id: string; name: string; active: boolean }[]; current?: CompanyMapping;
  selectedId: string; externalValue: string; onSelectedId(value: string): void; onExternalValue(value: string): void;
  onSave(): void; onCancel(): void; disabled: boolean; saving: boolean;
}) {
  const existingOption = current && !options.some((option) => option.id === current.internalValue)
    ? [{ id: current.internalValue, name: current.internalName ?? current.internalValue, active: current.internalActive ?? false }]
    : [];
  const availableOptions = [...existingOption, ...options];
  return <PrototypePanel title={account ? "Contas de recebimento" : "Categorias"}>
    <Stack spacing={2} sx={{ p: 2.5 }}>
      <Box sx={fieldGrid}>
        <TextField fullWidth size="small" label={account ? "Forma de pagamento interna" : "Categoria interna"}
          select value={selectedId} onChange={(event) => onSelectedId(event.target.value)} disabled={disabled}>
          <MenuItem value="">Selecione</MenuItem>
          {availableOptions.map((option) => <MenuItem key={option.id} value={option.id}>
            {option.name}{option.active ? "" : " (inativa)"}
          </MenuItem>)}
        </TextField>
        <TextField fullWidth size="small" label={account ? "Identificador da conta de recebimento no Nibo" : "Identificador da categoria no Nibo"}
          value={externalValue} onChange={(event) => onExternalValue(event.target.value)} disabled={disabled}
          helperText="Informe o identificador GUID existente no Nibo." autoComplete="off" />
      </Box>
      <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
        {current && <Button variant="outlined" disabled={saving} onClick={onCancel}>Cancelar edição</Button>}
        <Button variant="contained" disabled={disabled || saving || !selectedId || !externalValue.trim()}
          onClick={onSave}>{current ? "Salvar mapeamento" : "Adicionar mapeamento"}</Button>
      </Stack>
    </Stack>
  </PrototypePanel>;
}

export function NiboConnectionScreen({ id, reportBusy, reportDirty, onCreated }: {
  id: string | null; reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void; onCreated(id: string): void;
}) {
  const [section, setSection] = useState(0);
  const [filters, setFilters] = useState<NiboFilters>(defaultFilters);
  const [categoryId, setCategoryId] = useState("");
  const [categoryExternalValue, setCategoryExternalValue] = useState("");
  const [editingCategory, setEditingCategory] = useState<CompanyMapping | undefined>();
  const [accountId, setAccountId] = useState("");
  const [accountExternalValue, setAccountExternalValue] = useState("");
  const [editingAccount, setEditingAccount] = useState<CompanyMapping | undefined>();
  const [pendingRemoval, setPendingRemoval] = useState<string | null>(null);
  const query = useCompanyConnection(id, filters, id !== null);
  const categoryCatalog = useCompanyCatalog("categories", id !== null && section === 1);
  const paymentMethodCatalog = useCompanyCatalog("payment-methods", id !== null && section === 2);
  const activeCatalog = section === 1 ? categoryCatalog : paymentMethodCatalog;
  const mutations = useNiboConnectionMutations(id);
  const { showToast } = useToast();
  useQueryFeedback("nibo-connection-detail", query, reportBusy, "Não foi possível consultar a conexão Nibo.");
  useQueryFeedback("nibo-connection-catalog-" + section, activeCatalog, reportBusy,
    "Não foi possível carregar as opções internas do Nibo.");
  useReportMutationBusy("nibo-connection-mappings-" + (id ?? "new"),
    mutations.saveMapping.isPending || mutations.removeMapping.isPending, reportBusy);
  const detail = query.data as NiboDetail | undefined;

  const mappingColumns = (account: boolean): SelectableTableColumn<CompanyMapping>[] => [
    { id: "internalName", header: account ? "Forma de pagamento" : "Categoria da empresa",
      render: (row) => (row.internalName ?? (account ? row.internalValue : "Categoria indisponível"))
        + (row.internalActive === false ? " (inativa)" : "") },
    { id: "externalValue", header: account ? "Conta de recebimento no Nibo" : "Categoria no Nibo", render: (row) => row.externalValue },
    { id: "createdAt", header: "Criado em", render: (row) => dateText(row.createdAt) },
    { id: "updatedAt", header: "Alterado em", render: (row) => dateText(row.updatedAt) },
    { id: "actions", header: "Ações", sortable: false, render: (row) => <Stack direction="row" spacing={1}>
      <Button size="small" onClick={() => {
        if (account) { setEditingAccount(row); setAccountId(row.internalValue); setAccountExternalValue(row.externalValue); }
        else { setEditingCategory(row); setCategoryId(row.internalValue); setCategoryExternalValue(row.externalValue); }
      }}>Editar</Button>
      <Button size="small" color="error" onClick={() => setPendingRemoval(row.id)}>Remover</Button>
    </Stack> },
  ];

  function reportMappingDirty(type: "Category" | "ReceiptAccount", dirty: boolean) {
    reportDirty("nibo-connection-" + type, dirty);
  }

  function sortMapping(account: boolean, key: string) {
    setFilters((current) => account ? { ...current, receiptAccountPage: 1,
      receiptAccountSortBy: key as NiboFilters["receiptAccountSortBy"],
      receiptAccountSortDirection: current.receiptAccountSortBy === key && current.receiptAccountSortDirection === "asc" ? "desc" : "asc" }
      : { ...current, categoryPage: 1, categorySortBy: key as NiboFilters["categorySortBy"],
        categorySortDirection: current.categorySortBy === key && current.categorySortDirection === "asc" ? "desc" : "asc" });
  }

  async function saveMapping(account: boolean) {
    if (!id) return;
    const mapping: CompanyMappingDraft = account
      ? { internalId: accountId, externalValue: accountExternalValue }
      : { internalId: categoryId, externalValue: categoryExternalValue };
    try {
      await mutations.saveMapping.mutateAsync({
        type: account ? "ReceiptAccount" : "Category",
        mappingId: (account ? editingAccount : editingCategory)?.id ?? null,
        mapping,
      });
      if (account) { setEditingAccount(undefined); setAccountId(""); setAccountExternalValue(""); }
      else { setEditingCategory(undefined); setCategoryId(""); setCategoryExternalValue(""); }
      reportMappingDirty(account ? "ReceiptAccount" : "Category", false);
      showToast({ severity: "success", message: "Mapeamento salvo." });
    } catch (error) {
      showToast({ severity: "error", message: errorMessage(error, "Não foi possível salvar o mapeamento.") });
    }
  }

  async function removeMapping() {
    if (!pendingRemoval) return;
    try {
      await mutations.removeMapping.mutateAsync(pendingRemoval);
      setPendingRemoval(null);
      showToast({ severity: "success", message: "Mapeamento removido." });
    } catch (error) {
      setPendingRemoval(null);
      showToast({ severity: "error", message: errorMessage(error, "Não foi possível remover o mapeamento.") });
    }
  }

  return <>
    <ConnectionAdminFields id={id} provider="Nibo" defaultGroup="FinancialSystem" connection={detail?.connection}
      reportBusy={reportBusy} reportDirty={reportDirty} onCreated={onCreated} />
    {id && <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
      <Button variant="outlined" onClick={() => void query.refetch()}>{query.isError ? "Tentar novamente" : "Atualizar conexão"}</Button>
    </Stack>}
    <Tabs value={section} onChange={(_, nextSection: number) => setSection(nextSection)}
      aria-label="Configuração Nibo" variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tab id="nibo-tab-settings" aria-controls="nibo-panel-settings" label="Configuração" />
      <Tab id="nibo-tab-categories" aria-controls="nibo-panel-categories" label="Categorias" />
      <Tab id="nibo-tab-accounts" aria-controls="nibo-panel-accounts" label="Contas de recebimento" />
    </Tabs>
    <Box role="tabpanel" id="nibo-panel-settings" aria-labelledby="nibo-tab-settings" hidden={section !== 0}>
      <ProviderSettingsForm id={id} settings={detail?.settings} sections={niboSections}
        reportBusy={reportBusy} reportDirty={reportDirty} />
    </Box>
    <Box role="tabpanel" id="nibo-panel-categories" aria-labelledby="nibo-tab-categories" hidden={section !== 1}>
      <Stack spacing={2.5}>
        <MappingInputs account={false} options={categoryCatalog.data ?? []} current={editingCategory} selectedId={categoryId}
          externalValue={categoryExternalValue} onSelectedId={(value) => { setCategoryId(value); reportMappingDirty("Category", true); }}
          onExternalValue={(value) => { setCategoryExternalValue(value); reportMappingDirty("Category", true); }}
          onSave={() => void saveMapping(false)} onCancel={() => {
            setEditingCategory(undefined); setCategoryId(""); setCategoryExternalValue(""); reportMappingDirty("Category", false);
          }} disabled={!id || categoryCatalog.isPending || !detail} saving={mutations.saveMapping.isPending} />
        <ReadTable title="Categorias mapeadas" page={detail?.categoryMappings} columns={mappingColumns(false)} loaded={!!detail}
          sortBy={filters.categorySortBy} sortDirection={filters.categorySortDirection} onSort={(key) => sortMapping(false, key)}
          onPage={(page) => setFilters((current) => ({ ...current, categoryPage: page }))} />
      </Stack>
    </Box>
    <Box role="tabpanel" id="nibo-panel-accounts" aria-labelledby="nibo-tab-accounts" hidden={section !== 2}>
      <Stack spacing={2.5}>
        <MappingInputs account options={paymentMethodCatalog.data ?? []} current={editingAccount} selectedId={accountId}
          externalValue={accountExternalValue} onSelectedId={(value) => { setAccountId(value); reportMappingDirty("ReceiptAccount", true); }}
          onExternalValue={(value) => { setAccountExternalValue(value); reportMappingDirty("ReceiptAccount", true); }}
          onSave={() => void saveMapping(true)} onCancel={() => {
            setEditingAccount(undefined); setAccountId(""); setAccountExternalValue(""); reportMappingDirty("ReceiptAccount", false);
          }} disabled={!id || paymentMethodCatalog.isPending || !detail} saving={mutations.saveMapping.isPending} />
        <ReadTable title="Contas de recebimento mapeadas" page={detail?.receiptAccountMappings} columns={mappingColumns(true)} loaded={!!detail}
          sortBy={filters.receiptAccountSortBy} sortDirection={filters.receiptAccountSortDirection} onSort={(key) => sortMapping(true, key)}
          onPage={(page) => setFilters((current) => ({ ...current, receiptAccountPage: page }))} />
      </Stack>
    </Box>
    <Dialog open={pendingRemoval !== null} onClose={() => setPendingRemoval(null)} aria-labelledby="nibo-mapping-remove-title">
      <DialogTitle id="nibo-mapping-remove-title">Remover mapeamento?</DialogTitle>
      <DialogContent><Typography>O mapeamento será removido desta conexão Nibo.</Typography></DialogContent>
      <DialogActions>
        <Button variant="outlined" onClick={() => setPendingRemoval(null)}>Cancelar</Button>
        <Button color="error" variant="contained" disabled={mutations.removeMapping.isPending}
          onClick={() => void removeMapping()}>Remover</Button>
      </DialogActions>
    </Dialog>
  </>;
}
