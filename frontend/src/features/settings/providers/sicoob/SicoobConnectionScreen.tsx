"use client";

import { useState } from "react";
import { Box, Button, Stack, Tab, Tabs } from "@mui/material";
import { ConnectionAdminFields } from "../../components/shared/ConnectionAdminFields";
import { ProviderSettingsForm } from "../../components/shared/ProviderSettingsForm";
import { useQueryFeedback, type BusyReporter } from "../../components/shared/SettingsComponents";
import { useCompanyConnection } from "../../hooks/useCompanyConnection";
import { DEFAULT_DETAIL_FILTERS, type CompanyConnectionDetailFilter } from "../../types";
import { PaymentMethodBindings } from "../../components/shared/PaymentMethodBindings";
import { sicoobSections } from "./settingsFields";

export function SicoobConnectionScreen({ id, reportBusy, reportDirty, onCreated }: {
  id: string | null; reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void; onCreated(id: string): void;
}) {
  const [section, setSection] = useState(0);
  const [filters, setFilters] = useState<CompanyConnectionDetailFilter>(DEFAULT_DETAIL_FILTERS);
  const query = useCompanyConnection(id, filters, id !== null);
  useQueryFeedback("sicoob-connection-detail", query, reportBusy, "Não foi possível consultar a conexão Sicoob.");
  const detail = query.data;

  return <>
    <ConnectionAdminFields id={id} provider="Sicoob" defaultGroup="PaymentProvider" connection={detail?.connection}
      reportBusy={reportBusy} reportDirty={reportDirty} onCreated={onCreated} />
    {id && <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
      <Button variant="outlined" onClick={() => void query.refetch()}>{query.isError ? "Tentar novamente" : "Atualizar conexão"}</Button>
    </Stack>}
    <Tabs value={section} onChange={(_, nextSection: number) => setSection(nextSection)}
      aria-label="Configuração Sicoob" variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tab id="sicoob-tab-settings" aria-controls="sicoob-panel-settings" label="Configuração" />
      <Tab id="sicoob-tab-payment-methods" aria-controls="sicoob-panel-payment-methods" label="Formas de pagamento" />
    </Tabs>
    <Box role="tabpanel" id="sicoob-panel-settings" aria-labelledby="sicoob-tab-settings" hidden={section !== 0}>
      <ProviderSettingsForm id={id} settings={detail?.settings} sections={sicoobSections}
        reportBusy={reportBusy} reportDirty={reportDirty} />
    </Box>
    <Box role="tabpanel" id="sicoob-panel-payment-methods" aria-labelledby="sicoob-tab-payment-methods" hidden={section !== 1}>
      <PaymentMethodBindings id={id} detail={detail} filters={filters} onFilters={setFilters}
        reportBusy={reportBusy} reportDirty={reportDirty} />
    </Box>
  </>;
}
