"use client";

import { useState } from "react";
import { Box, Button, Stack } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useToast } from "@/providers/ToastProvider";
import { useSaveConnectionSettings } from "../../hooks/useSaveConnectionSettings";
import type { CompanyConnectionSetting } from "../../types";
import type { SettingsSection } from "../../settingsFields";
import { errorMessage, fieldGrid, SettingsInput, useReportMutationBusy, type BusyReporter } from "./SettingsComponents";

export function ProviderSettingsForm({ id, settings, sections, reportBusy, reportDirty }: {
  id: string | null; settings?: CompanyConnectionSetting[]; sections: readonly SettingsSection[];
  reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void;
}) {
  const mutation = useSaveConnectionSettings(id);
  const { showToast } = useToast();
  const [draftState, setDraftState] = useState<{ source: CompanyConnectionSetting[] | undefined; values: Record<string, string> }>(
    { source: undefined, values: {} });
  const [dirty, setDirty] = useState(false);
  const resource = "connection-settings";
  const persistedValues = Object.fromEntries((settings ?? []).map((setting) =>
    [`${setting.group}.${setting.key}`, setting.value ?? ""]));
  const draftValues = draftState.source === settings ? draftState.values : persistedValues;
  useReportMutationBusy(`save-provider-settings-${id ?? "new"}`, mutation.isPending, reportBusy);

  function changeSetting(key: string, value: string) {
    setDraftState({ source: settings, values: { ...draftValues, [key]: value } });
    setDirty(true);
    reportDirty(resource, true);
  }

  function discard() {
    setDraftState({ source: settings, values: persistedValues });
    setDirty(false);
    reportDirty(resource, false);
  }

  async function save() {
    if (!id || !settings) return;
    const inputs = sections.flatMap((section) => section.fields)
      .filter((field) => field.source)
      .map((field) => {
        const source = field.source!;
        const key = `${source.group}.${source.key}`;
        return { group: source.group, key: source.key, value: draftValues[key] ?? persistedValues[key] ?? "" };
      });
    try {
      await mutation.mutateAsync(inputs);
      setDirty(false);
      reportDirty(resource, false);
      showToast({ severity: "success", message: "Configurações do provider salvas." });
    } catch (error) {
      showToast({ severity: "error", message: errorMessage(error, "Não foi possível salvar as configurações do provider.") });
    }
  }

  const disabled = id !== null && !settings;
  return <>
    {sections.map((section) => <PrototypePanel key={section.title} title={section.title}>
      <Box sx={{ ...fieldGrid, p: 2.5 }}>
        {section.fields.map((field) => {
          const settingKey = field.source ? `${field.source.group}.${field.source.key}` : "";
          const persisted = field.source ? settings?.find((setting) =>
            setting.group === field.source?.group && setting.key === field.source?.key)?.value : undefined;
          return <SettingsInput key={field.id} field={field} persisted={persisted}
            value={draftValues[settingKey] ?? persisted ?? ""}
            onChange={(value) => changeSetting(settingKey, value)} disabled={disabled} />;
        })}
      </Box>
    </PrototypePanel>)}
    <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
      <Button variant="outlined" disabled={!dirty || mutation.isPending} onClick={discard}>Cancelar parâmetros</Button>
      <Button variant="contained" disabled={!dirty || mutation.isPending || !settings}
        onClick={() => void save()}>Salvar parâmetros do provider</Button>
    </Stack>
  </>;
}
