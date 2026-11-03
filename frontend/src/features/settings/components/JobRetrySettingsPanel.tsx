"use client";
import { Box, Button, Stack, TextField, Typography } from "@mui/material";
import { useEffect, useState } from "react";
import { useCompanySettings, useSaveCompanySettings } from "../hooks/useCompanySettings";
import { useToast } from "@/providers/ToastProvider";
import { useQueryFeedback, useReportMutationBusy, type BusyReporter } from "./shared/SettingsComponents";

// Tipos de Job com retry automatico, canonicos no backend (JobRetryTypes).
const JOB_RETRY_TYPES: { type: string; label: string }[] = [
  { type: "FINANCIAL_CHARGE_CREATE_V1", label: "Criação de cobrança financeira" },
  { type: "FINANCIAL_CHARGE_SETTLE_V1", label: "Baixa de cobrança" },
  { type: "FINANCIAL_DEBIT_INBOUND_V1", label: "Débito recebido (inbound)" },
];
const MAXIMUM_RETRIES = 20;

export function JobRetrySettingsPanel({ reportBusy }: { reportBusy: BusyReporter }) {
  const { showToast } = useToast();
  const query = useCompanySettings("job-retry", true);
  const save = useSaveCompanySettings("job-retry");
  const [draft, setDraft] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!query.data) return;
    setDraft(Object.fromEntries(JOB_RETRY_TYPES.map(({ type }) => [type, String(query.data.limits[type] ?? 0)])));
  }, [query.data]);

  useQueryFeedback("job-retry-query", query, reportBusy, "Não foi possível carregar as configurações de Jobs.");
  useReportMutationBusy("job-retry-save", save.isPending, reportBusy);

  function handleSave() {
    const limits = Object.fromEntries(JOB_RETRY_TYPES.map(({ type }) => [type, Number(draft[type] ?? 0)]));
    save.mutate({ limits }, {
      onSuccess: () => showToast({ severity: "success", message: "Configurações de Jobs salvas." }),
      onError: () => showToast({ severity: "error", message: "Não foi possível salvar as configurações de Jobs. Tente novamente." }),
    });
  }

  return (
    <Stack spacing={2.5}>
      <Typography sx={{ color: "#5b6d91" }}>
        Retentativas automáticas por tipo de Job. Zero desativa o reprocessamento automático.
      </Typography>
      {JOB_RETRY_TYPES.map(({ type, label }) => (
        <TextField key={type} label={label} type="number" value={draft[type] ?? "0"} disabled={!query.data}
          onChange={(event) => setDraft((current) => ({ ...current, [type]: event.target.value }))}
          slotProps={{ htmlInput: { min: 0, max: MAXIMUM_RETRIES, step: 1 } }}
          helperText={`De 0 a ${MAXIMUM_RETRIES} retentativas. Tipo: ${type}`} />
      ))}
      <Box>
        <Button variant="contained" onClick={handleSave} disabled={!query.data || save.isPending}>Salvar configurações de Jobs</Button>
      </Box>
    </Stack>
  );
}
