"use client";

import { Button, Stack, Typography } from "@mui/material";
import { useCompanyConnection } from "../../hooks/useCompanyConnection";
import { DEFAULT_DETAIL_FILTERS } from "@/services/api/settings/types";
import { ConnectionAdminFields } from "./ConnectionAdminFields";
import { useQueryFeedback, type BusyReporter } from "./SettingsComponents";

export function UnsupportedConnectionScreen({
  provider,
  id,
  reportBusy,
  reportDirty,
  onCreated,
}: {
  provider: string;
  id: string | null;
  reportBusy: BusyReporter;
  reportDirty(resource: string, dirty: boolean): void;
  onCreated(id: string): void;
}) {
  const query = useCompanyConnection(id, DEFAULT_DETAIL_FILTERS, id !== null);
  useQueryFeedback(
    "unsupported-connection-detail",
    query,
    reportBusy,
    "Não foi possível consultar a conexão.",
  );
  return (
    <>
      <ConnectionAdminFields
        id={id}
        provider={provider}
        defaultGroup={query.data?.connection.group ?? ""}
        connection={query.data?.connection}
        reportBusy={reportBusy}
        reportDirty={reportDirty}
        onCreated={onCreated}
      />
      {id && (
        <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
          <Button variant="outlined" onClick={() => void query.refetch()}>
            {query.isError ? "Tentar novamente" : "Atualizar conexão"}
          </Button>
        </Stack>
      )}
      <Typography color="text.secondary">
        Os dados cadastrais estão disponíveis. Este provider ainda não possui formulário de
        configuração.
      </Typography>
    </>
  );
}
