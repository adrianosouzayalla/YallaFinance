"use client";

import { Alert, Button } from "@mui/material";
import { SuccessStatusIcon } from "@/components/ui/SuccessStatusIcon";
import type { ChargeProcessing } from "@/services/api/charges/charge.types";

export function ChargeProcessingPanel({
  processing,
  confirmedItems,
  totalItems,
  refreshing,
  onRefresh,
}: {
  processing: ChargeProcessing;
  confirmedItems: number;
  totalItems: number;
  refreshing: boolean;
  onRefresh?: () => void;
}) {
  const result = processing.result;
  let message: string;
  let severity: "info" | "success" | "warning" | "error";
  switch (processing.status) {
    case "Pending":
      message = "Integração agendada.";
      severity = "info";
      break;
    case "Processing":
      message = "Integração em processamento.";
      severity = "info";
      break;
    case "Success":
      message = "Integração concluída.";
      severity = "success";
      break;
    case "NotScheduled":
      message = "Cobrança criada, mas a integração não foi agendada.";
      severity = "error";
      break;
    default:
      severity = "warning";
      if (result?.indeterminateItems) {
        message = `Integração com resultado incerto: ${result.confirmedItems} de ${result.totalItems} itens confirmados. Confira os detalhes antes de qualquer nova tentativa.`;
      } else if (result?.confirmedItems) {
        message = `Integração parcialmente concluída: ${result.confirmedItems} de ${result.totalItems} itens confirmados.`;
      } else if (!result && confirmedItems > 0) {
        message = `Integração interrompida com ${confirmedItems} de ${totalItems} itens confirmados. Os demais itens precisam de verificação.`;
      } else {
        const reasons: Record<string, string> = {
          ChargeNotFound: "Cobrança não encontrada no processamento.",
          InvalidChargeItems: "Não foi possível validar os itens da cobrança.",
          InvalidChargePayload: "Não foi possível identificar a cobrança para processamento.",
          JobHandlerError: "O processamento foi interrompido por uma falha.",
          UnknownJobType: "O serviço não reconheceu a operação agendada.",
          InvalidJobPayload: "Não foi possível recuperar os dados da operação agendada.",
          InvalidHandlerResult: "Não foi possível confirmar o resultado do processamento.",
        };
        message = `Integração falhou. ${reasons[processing.errorCode ?? ""] ?? "Confira os lançamentos financeiros antes de uma nova tentativa."} A cobrança permanece registrada.`;
      }
  }
  return (
    <Alert
      severity={severity}
      iconMapping={{ success: <SuccessStatusIcon fontSize="inherit" /> }}
      action={
        onRefresh ? (
          <Button disabled={refreshing} onClick={onRefresh}>
            Atualizar integração
          </Button>
        ) : undefined
      }
    >
      {message}
    </Alert>
  );
}
