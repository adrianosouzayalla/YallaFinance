"use client";

import { useState } from "react";
import { Box, Button, Stack, Tab, Tabs } from "@mui/material";
import { PaymentMethodBindings } from "../../components/shared/PaymentMethodBindings";
import { ConnectionAdminFields } from "../../components/shared/ConnectionAdminFields";
import { ProviderSettingsForm } from "../../components/shared/ProviderSettingsForm";
import { useQueryFeedback, type BusyReporter } from "../../components/shared/SettingsComponents";
import { useCompanyConnection } from "../../hooks/useCompanyConnection";
import { DEFAULT_DETAIL_FILTERS } from "../../types";
import { asaasSections } from "./settingsFields";

export function AsaasConnectionScreen({ id, reportBusy, reportDirty, onCreated }: {
  id: string | null; reportBusy: BusyReporter; reportDirty(resource: string, dirty: boolean): void; onCreated(id: string): void;
}) {
  const [section, setSection] = useState(0);
  const [filters, setFilters] = useState(DEFAULT_DETAIL_FILTERS);
  const query = useCompanyConnection(id, filters, id !== null);
  useQueryFeedback("asaas-connection-detail", query, reportBusy, "Não foi possível consultar a conexão Asaas.");
  return <>
    <ConnectionAdminFields id={id} provider="Asaas" defaultGroup="PaymentProvider" connection={query.data?.connection}
      reportBusy={reportBusy} reportDirty={reportDirty} onCreated={onCreated} />
    {id && <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
      <Button variant="outlined" onClick={() => void query.refetch()}>{query.isError ? "Tentar novamente" : "Atualizar conexão"}</Button>
    </Stack>}
    <Tabs value={section} onChange={(_, nextSection: number) => setSection(nextSection)}
      aria-label="Configuração Asaas" variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tab id="asaas-tab-settings" aria-controls="asaas-panel-settings" label="Configuração" />
      <Tab id="asaas-tab-payment-methods" aria-controls="asaas-panel-payment-methods" label="Formas de pagamento" />
    </Tabs>
    <Box role="tabpanel" id="asaas-panel-settings" aria-labelledby="asaas-tab-settings" hidden={section !== 0}>
      <ProviderSettingsForm id={id} settings={query.data?.settings} sections={asaasSections}
        reportBusy={reportBusy} reportDirty={reportDirty} />
    </Box>
    <Box role="tabpanel" id="asaas-panel-payment-methods" aria-labelledby="asaas-tab-payment-methods" hidden={section !== 1}>
      <PaymentMethodBindings id={id} detail={query.data} filters={filters} onFilters={setFilters}
        reportBusy={reportBusy} reportDirty={reportDirty} />
    </Box>
  </>;
}
