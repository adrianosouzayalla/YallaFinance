"use client";

import { useCallback, useState, type ReactNode } from "react";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Menu, MenuItem,
  Stack, Tab, Tabs, TextField, Typography } from "@mui/material";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { LoadingState } from "@/components/ui/LoadingState";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useToast } from "@/providers/ToastProvider";
import { useCompanySettings, useCompanyConnections, useSaveCompanySettings } from "../hooks/useCompanySettings";
import { companySections, type SettingsField, type SettingsSection } from "../settingsFields";
import { ConnectionProviderHost } from "./ConnectionProviderHost";
import { errorMessage, fieldGrid, groupLabel, ReadTable, SettingsInput, useQueryFeedback, useReportMutationBusy, type BusyReporter }
  from "./shared/SettingsComponents";
import { DEFAULT_CONNECTION_FILTERS, type CompanyConnectionFilter, type CompanyConnectionSummary,
  type CompanySettingsByGroup } from "../types";
import { JobRetrySettingsPanel } from "./JobRetrySettingsPanel";

type SettingsView = { area: "company" | "connections" | "jobs" } | { area: "connection"; provider: string; id: string | null };
const isInvalidCompanyField = (field: SettingsField, value: string) => {
  if (field.required && value.trim() === "") return true;
  if (field.kind !== "number" || value === "") return false;
  return !/^\d+$/.test(value) || !Number.isSafeInteger(Number(value))
    || (field.min !== undefined && Number(value) < field.min)
    || (field.max !== undefined && Number(value) > field.max);
};

function CompanyGroup({ section, reportBusy, reportDirty }: { section: SettingsSection; reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void }) {
  const group = section.companyGroup!;
  const apiGroup = group === "regional-settings" ? "financial-settlement" : group;
  const query = useCompanySettings(apiGroup, true);
  const mutation = useSaveCompanySettings(apiGroup);
  const { showToast } = useToast();
  const [draft, setDraft] = useState<{ source: CompanySettingsByGroup[typeof apiGroup] | undefined; values: Record<string, string> }>({ source: undefined, values: {} });
  const [dirty, setDirty] = useState(false);
  useReportMutationBusy(`save-settings-${group}`, mutation.isPending, reportBusy);
  useQueryFeedback(`settings-${group}`, query, reportBusy, `Não foi possível consultar ${section.title}.`);
  const data = query.data;
  const values: Record<string, string | null> = {};
  if (group === "payments" && data) {
    const payments = data as CompanySettingsByGroup["payments"];
    values["EMP-01"] = payments.maximumLinkDays; values["EMP-02"] = payments.publicOrigin;
  }
  if (group === "session" && data) values["EMP-03"] = (data as CompanySettingsByGroup["session"]).sessionLifetimeMinutes;
  if (group === "security" && data) {
    const security = data as CompanySettingsByGroup["security"];
    values["EMP-06"] = security.loginMaxFailedAttempts; values["EMP-07"] = security.loginAttemptWindowMinutes;
  }
  if (group === "regional-settings" && data)
    values["EMP-05"] = (data as CompanySettingsByGroup["financial-settlement"]).receiptTimeZoneId;
  if (group === "user-registration" && data)
    values["EMP-04"] = (data as CompanySettingsByGroup["user-registration"]).initialPassword;
  const draftValues = draft.source === data ? draft.values
    : Object.fromEntries(Object.entries(values).map(([key, value]) => [key, value ?? ""]));
  const hasInvalidValues = section.fields.some((field) =>
    isInvalidCompanyField(field, draftValues[field.id] ?? values[field.id] ?? ""));
  async function save() {
    let payload: Partial<CompanySettingsByGroup[typeof apiGroup]>;
    if (group === "payments") payload = {
      maximumLinkDays: draftValues["EMP-01"] ?? values["EMP-01"] ?? "",
      publicOrigin: draftValues["EMP-02"] ?? values["EMP-02"] ?? "",
    } as CompanySettingsByGroup["payments"];
    else if (group === "session") payload = { sessionLifetimeMinutes: draftValues["EMP-03"] ?? values["EMP-03"] ?? "" } as CompanySettingsByGroup["session"];
    else if (group === "security") payload = {
      loginMaxFailedAttempts: draftValues["EMP-06"] ?? values["EMP-06"] ?? "",
      loginAttemptWindowMinutes: draftValues["EMP-07"] ?? values["EMP-07"] ?? "",
    } as CompanySettingsByGroup["security"];
    else if (group === "user-registration") payload = { initialPassword: draftValues["EMP-04"] ?? values["EMP-04"] ?? "" } as CompanySettingsByGroup["user-registration"];
    else payload = { receiptTimeZoneId: draftValues["EMP-05"] ?? values["EMP-05"] ?? "" } as CompanySettingsByGroup["financial-settlement"];
    try {
      await mutation.mutateAsync(payload);
      setDirty(false); reportDirty(`settings-${group}`, false);
      await query.refetch();
      showToast({ severity: "success", message: `${section.title} salva.` });
    } catch (error) {
      showToast({ severity: "error", message: errorMessage(error, `Não foi possível salvar ${section.title}.`) });
    }
  }
  function discard() {
    setDraft({ source: data, values: Object.fromEntries(Object.entries(values).map(([key, value]) => [key, value ?? ""])) });
    setDirty(false); reportDirty(`settings-${group}`, false);
  }
  return <PrototypePanel title={section.title} action={<Stack direction="row" spacing={1}>
    <Button size="small" onClick={() => void query.refetch()} aria-label={`${query.isError ? "Tentar novamente" : "Atualizar"} ${section.title}`}>
      {query.isError ? "Tentar novamente" : "Atualizar"}</Button>
    <Button size="small" variant="outlined" disabled={!dirty || mutation.isPending} onClick={discard}>Cancelar</Button>
    <Button size="small" variant="contained" disabled={!dirty || mutation.isPending || !data || hasInvalidValues} onClick={() => void save()}>Salvar</Button>
  </Stack>}>
    <Box id={`company-group-${group}`}>
      {section.description ? <Typography variant="body2" color="text.secondary" sx={{ px: 2.5, pt: 2 }}>{section.description}</Typography> : null}
      <Box sx={{ ...fieldGrid, p: 2.5 }}>{section.fields.map((field) =>
        <SettingsInput key={field.id} field={field} persisted={values[field.id]} value={draftValues[field.id] ?? values[field.id] ?? ""}
          validateRequired
          onChange={(value) => { setDraft({ source: data, values: { ...draftValues, [field.id]: value } }); setDirty(true); reportDirty(`settings-${group}`, true); }} disabled={!data} />)}</Box>
    </Box>
  </PrototypePanel>;
}

function ConnectionsList({ onOpen, onCreate, reportBusy }: {
  onOpen(connection: CompanyConnectionSummary): void; onCreate(provider: string): void; reportBusy: BusyReporter;
}) {
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [filters, setFilters] = useState<CompanyConnectionFilter>({ ...DEFAULT_CONNECTION_FILTERS });
  const [search, setSearch] = useState("");
  const [provider, setProvider] = useState("");
  const [group, setGroup] = useState("");
  const [enabled, setEnabled] = useState("");
  const query = useCompanyConnections(filters, true);
  useQueryFeedback("connection-list", query, reportBusy, "Não foi possível consultar as conexões.");
  function applyFilters(next: CompanyConnectionFilter) {
    if (JSON.stringify(next) === JSON.stringify(filters)) void query.refetch();
    else setFilters(next);
  }
  return <Stack spacing={2.5}>
    <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}>
      <Typography component="h2" variant="h2" color="secondary">Conexões da empresa</Typography>
      <Stack direction="row" spacing={1.5}>
        <Button variant="outlined" onClick={() => void query.refetch()}>{query.isError ? "Tentar novamente" : "Atualizar"}</Button>
        <Button variant="contained" startIcon={<AddOutlinedIcon />} aria-haspopup="menu"
          aria-controls={menuAnchor ? "new-connection-menu" : undefined} aria-expanded={Boolean(menuAnchor)}
          onClick={(event) => setMenuAnchor(event.currentTarget)}>Nova conexão</Button>
      </Stack>
    </Stack>
    <Stack component="form" direction="row" sx={{ alignItems: "flex-end", columnGap: 1.25, flexWrap: "wrap", rowGap: 2 }}
      onSubmit={(event) => { event.preventDefault(); applyFilters({ ...filters, page: 1, search: search || undefined,
        provider: provider || undefined, group: group || undefined, enabled: enabled === "" ? undefined : enabled === "true" }); }} autoComplete="off">
      <TextField label="Nome da conexão" size="small" value={search} onChange={(event) => setSearch(event.target.value)} />
      <TextField label="Provider" size="small" select value={provider} onChange={(event) => setProvider(event.target.value)} sx={{ minWidth: 140 }}>
        <MenuItem value="">Todos</MenuItem><MenuItem value="Nibo">Nibo</MenuItem><MenuItem value="Sicoob">Sicoob</MenuItem><MenuItem value="Asaas">Asaas</MenuItem>
      </TextField>
      <TextField label="Grupo" size="small" select value={group} onChange={(event) => setGroup(event.target.value)} sx={{ minWidth: 160 }}>
        <MenuItem value="">Todos</MenuItem><MenuItem value="FinancialSystem">Sistema financeiro</MenuItem><MenuItem value="PaymentProvider">Provider de pagamento</MenuItem>
      </TextField>
      <TextField label="Situação" size="small" select value={enabled} onChange={(event) => setEnabled(event.target.value)} sx={{ minWidth: 140 }}>
        <MenuItem value="">Todas</MenuItem><MenuItem value="true">Ativa</MenuItem><MenuItem value="false">Inativa</MenuItem>
      </TextField>
      <Button variant="contained" type="submit">Filtrar</Button>
      <Button variant="outlined" onClick={() => { setSearch(""); setProvider(""); setGroup(""); setEnabled(""); applyFilters({ ...DEFAULT_CONNECTION_FILTERS }); }}>Limpar</Button>
    </Stack>
    <ReadTable title="Conexões cadastradas" page={query.data} loaded={!!query.data}
      columns={[
        { id: "name", header: "Nome", render: (row) => row.name },
        { id: "provider", header: "Provider", render: (row) => row.provider },
        { id: "group", header: "Grupo", render: (row) => <StatusBadge label={groupLabel(row.group)}
          tone={row.group === "FinancialSystem" ? "info" : row.group === "PaymentProvider" ? "orange" : "default"} /> },
        { id: "enabled", header: "Situação", render: (row) => <StatusBadge label={row.enabled ? "Ativa" : "Inativa"}
          tone={row.enabled ? "success" : "default"} /> },
        { id: "actions", header: "Ações", sortable: false, render: (row) =>
          <Button size="small" aria-label={`Abrir conexão ${row.name}`} onClick={() => onOpen(row)}>Abrir</Button> },
      ]} sortBy={filters.sortBy} sortDirection={filters.sortDirection}
      onSort={(key) => setFilters((current) => ({ ...current, page: 1, sortBy: key as CompanyConnectionFilter["sortBy"],
        sortDirection: current.sortBy === key && current.sortDirection === "asc" ? "desc" : "asc" }))}
      onPage={(page) => setFilters((current) => ({ ...current, page }))} />
    <Menu id="new-connection-menu" anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={() => setMenuAnchor(null)}>
      {["Nibo", "Sicoob", "Asaas"].map((name) => <MenuItem key={name} onClick={() => { setMenuAnchor(null); onCreate(name); }}>
        {name} — {name === "Nibo" ? "Sistema financeiro" : name === "Sicoob" ? "Pagamentos Pix" : "Provedor de pagamento"}</MenuItem>)}
    </Menu>
  </Stack>;
}

function CompanySettingsScreen() {
  const [view, setView] = useState<SettingsView>({ area: "company" });
  const [pendingView, setPendingView] = useState<SettingsView | null>(null);
  const [dirtyResources, setDirtyResources] = useState<Record<string, boolean>>({});
  const dirty = Object.values(dirtyResources).some(Boolean);
  const [formVersion, setFormVersion] = useState(0);
  const [busyResources, setBusyResources] = useState<Record<string, boolean>>({});
  const reportBusy = useCallback<BusyReporter>((resource, busy) =>
    setBusyResources((current) => current[resource] === busy ? current : { ...current, [resource]: busy }), []);
  const reportDirty = useCallback((resource: string, isDirty: boolean) =>
    setDirtyResources((current) => current[resource] === isDirty ? current : { ...current, [resource]: isDirty }), []);
  function discardChanges(nextView: SettingsView) {
    setView(nextView); setDirtyResources({}); setPendingView(null); setFormVersion((version) => version + 1);
  }
  function requestView(nextView: SettingsView) {
    if (dirty) setPendingView(nextView);
    else discardChanges(nextView);
  }
  const content: ReactNode = view.area === "connection"
    ? <ConnectionProviderHost provider={view.provider} id={view.id} reportBusy={reportBusy} reportDirty={reportDirty}
      onCreated={(id) => setView({ area: "connection", provider: view.provider, id })} />
    : <>{companySections.map((section) => <CompanyGroup key={section.companyGroup} section={section} reportBusy={reportBusy} reportDirty={reportDirty} />)}</>;
  return <Stack spacing={2.5}>
    <LoadingState mode="blocking" open={Object.values(busyResources).some(Boolean)} message="Carregando ou salvando configurações..." />
    <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}>
      <FinancePageTitle navigationHref="/settings">{view.area === "connection" ? `Conexão ${view.provider}` : "Configurações do sistema"}</FinancePageTitle>
      {view.area === "connection" && <Button variant="outlined" startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => requestView({ area: "connections" })}>Voltar para conexões</Button>}
    </Stack>
    <Typography color="text.secondary">{view.area === "connection" ? "Parâmetros e relacionamentos desta conexão." : "Gerencie os parâmetros da empresa e a configuração de cada conexão."}</Typography>
    {view.area !== "connection" && <Tabs value={view.area} onChange={(_, area: "company" | "connections" | "jobs") => requestView({ area })}
      variant="scrollable" scrollButtons="auto" aria-label="Áreas de configuração" sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tab value="company" label="Empresa" id="settings-tab-company" aria-controls="settings-panel-company" />
      <Tab value="connections" label="Conexões" id="settings-tab-connections" aria-controls="settings-panel-connections" />
      <Tab value="jobs" label="Jobs" id="settings-tab-jobs" aria-controls="settings-panel-jobs" />
    </Tabs>}
    {view.area === "jobs" ? <Box role="tabpanel" id="settings-panel-jobs" aria-labelledby="settings-tab-jobs">
      <JobRetrySettingsPanel reportBusy={reportBusy} />
    </Box> : view.area === "connections" ? <Box role="tabpanel" id="settings-panel-connections" aria-labelledby="settings-tab-connections">
      <ConnectionsList reportBusy={reportBusy} onCreate={(provider) => requestView({ area: "connection", provider, id: null })}
        onOpen={(connection) => requestView({ area: "connection", provider: connection.provider, id: connection.id })} />
    </Box> : <Stack component="form" key={formVersion} spacing={2.5}
      role={view.area === "company" ? "tabpanel" : undefined} id={`settings-panel-${view.area}`}
      aria-labelledby={view.area === "company" ? "settings-tab-company" : undefined}
      aria-label={view.area === "connection" ? `Formulário da conexão ${view.provider}` : undefined}
      onSubmit={(event) => event.preventDefault()} autoComplete="off">
      {content}
    </Stack>}
    <Dialog open={pendingView !== null} onClose={() => setPendingView(null)} aria-labelledby="settings-discard-title">
      <DialogTitle id="settings-discard-title">Descartar alterações?</DialogTitle>
      <DialogContent><Typography>Os campos preenchidos ainda não foram salvos. Deseja descartá-los?</Typography></DialogContent>
      <DialogActions><Button variant="outlined" onClick={() => setPendingView(null)}>Continuar editando</Button>
        <Button variant="contained" onClick={() => { if (pendingView) discardChanges(pendingView); }}>Descartar alterações</Button>
      </DialogActions>
    </Dialog>
  </Stack>;
}

export function SettingsPage() {
  const { tenantId, contextId } = useSessionContext();
  return <CompanySettingsScreen key={`${tenantId}:${contextId}`} />;
}
