"use client";

import { useState } from "react";
import { Box, Button, FormControlLabel, Stack, Switch, TextField, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useToast } from "@/providers/ToastProvider";
import { useConnectionAdminMutations } from "../../hooks/useConnectionAdminMutations";
import type { CompanyConnectionSummary } from "../../types";
import { errorMessage, fieldGrid, groupLabel, useReportMutationBusy, type BusyReporter } from "./SettingsComponents";

export function ConnectionAdminFields({ id, provider, defaultGroup, connection, reportBusy, reportDirty, onCreated }: {
  id: string | null; provider: string; defaultGroup: string; connection?: CompanyConnectionSummary;
  reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void; onCreated(id: string): void;
}) {
  const mutations = useConnectionAdminMutations(id);
  const { showToast } = useToast();
  const adminResource = "connection-admin";
  const [draftState, setDraftState] = useState<{ source: CompanyConnectionSummary | undefined; values: {
    name: string; enabled: boolean;
  } }>({ source: undefined, values: { name: "", enabled: false } });
  const [dirty, setDirty] = useState(false);
  const draft = draftState.source === connection ? draftState.values
    : connection ? { name: connection.name, enabled: connection.enabled }
      : { name: "", enabled: false };
  const disabled = id !== null && !connection;
  useReportMutationBusy(`save-connection-admin-${id ?? "new"}`,
    mutations.createConnection.isPending || mutations.saveConnection.isPending, reportBusy);

  function changeAdmin(update: Partial<typeof draft>) {
    setDraftState({ source: connection, values: { ...draft, ...update } });
    setDirty(true);
    reportDirty(adminResource, true);
  }

  function discard() {
    setDraftState({ source: connection, values: connection
      ? { name: connection.name, enabled: connection.enabled }
      : { name: "", enabled: false } });
    setDirty(false);
    reportDirty(adminResource, false);
  }

  async function save() {
    const payload = { ...draft, provider, group: connection?.group ?? defaultGroup };
    try {
      if (id) {
        await mutations.saveConnection.mutateAsync(payload);
        setDirty(false);
        reportDirty(adminResource, false);
        showToast({ severity: "success", message: "Conexão salva." });
      } else {
        const createdId = await mutations.createConnection.mutateAsync(payload);
        setDirty(false);
        reportDirty(adminResource, false);
        onCreated(createdId);
        showToast({ severity: "success", message: "Conexão criada. Agora salve os parâmetros do provider." });
      }
    } catch (error) {
      showToast({ severity: "error", message: errorMessage(error, "Não foi possível salvar a conexão.") });
    }
  }

  return <>
    <PrototypePanel title="Identificação da conexão" action={<Stack direction="row" spacing={1}>
      <Button size="small" variant="outlined" disabled={!dirty || mutations.createConnection.isPending || mutations.saveConnection.isPending}
        onClick={discard}>Cancelar</Button>
      <Button size="small" variant="contained" disabled={!dirty || !draft.name.trim() || disabled
        || mutations.createConnection.isPending || mutations.saveConnection.isPending} onClick={() => void save()}>
        {id ? "Salvar conexão" : "Criar conexão"}
      </Button>
    </Stack>}>
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Box sx={fieldGrid}>
          <TextField label="Nome da conexão" required value={draft.name}
            onChange={(event) => changeAdmin({ name: event.target.value })}
            disabled={disabled} fullWidth size="small" autoComplete="off" />
          <TextField label="Provider" value={provider} fullWidth size="small" slotProps={{ input: { readOnly: true } }} />
          <TextField label="Grupo da conexão" value={groupLabel(connection?.group ?? defaultGroup)}
            fullWidth size="small" slotProps={{ input: { readOnly: true } }} />
        </Box>
        <Stack direction="row" spacing={3} sx={{ flexWrap: "wrap" }}>
          <FormControlLabel control={<Switch disabled={disabled} checked={draft.enabled}
            onChange={(_, checked) => changeAdmin({ enabled: checked })} />} label="Conexão ativa" />
        </Stack>
        <Typography color="text.secondary">Uma conexão de sistema financeiro ativa por empresa. Providers de pagamento ativos são selecionados pelo vínculo com a forma de pagamento.</Typography>
      </Stack>
    </PrototypePanel>
    {connection && <PrototypePanel title="Identificação e auditoria"><Box sx={{ ...fieldGrid, p: 2.5 }}>
      <TextField label="Identificador da conexão" value={connection.id} fullWidth size="small" slotProps={{ input: { readOnly: true } }} />
      <TextField label="Criado em" value={connection.createdAt ? new Date(connection.createdAt).toLocaleString("pt-BR") : "—"}
        fullWidth size="small" slotProps={{ input: { readOnly: true } }} />
      <TextField label="Alterado em" value={connection.updatedAt ? new Date(connection.updatedAt).toLocaleString("pt-BR") : "—"}
        fullWidth size="small" slotProps={{ input: { readOnly: true } }} />
    </Box></PrototypePanel>}
  </>;
}
