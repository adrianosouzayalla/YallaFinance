import type { JobStatus } from "../types/job.types";
export const jobStatusLabels: Record<JobStatus, string> = {
  Pending: "Aguardando",
  Processing: "Em processamento",
  Success: "Sucesso",
  Failed: "Erro",
};
export function formatJobDate(value: string | null | undefined) {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleString("pt-BR");
}
export function formatJobDuration(seconds: number | null | undefined) {
  if (seconds == null || !Number.isFinite(seconds)) return "—";
  const value = Math.max(0, Math.floor(seconds));
  if (value < 60) return `${value}s`;
  if (value < 3600) return `${Math.floor(value / 60)}min ${value % 60}s`;
  return `${Math.floor(value / 3600)}h ${Math.floor((value % 3600) / 60)}min`;
}
export function jobErrorMessage(code: string) {
  const messages: Record<string, string> = {
    Unauthorized: "Execução sem autorização.",
    ChargeIntegrationPartial: "A execução teve resultado parcial.",
    ChargeIntegrationFailed: "A execução não confirmou todas as operações.",
    UnknownJobType: "O tipo de Job não está registrado no Worker.",
    InvalidJobPayload: "Os dados do Job são inválidos.",
    InvalidChargePayload: "Os dados do Job são inválidos.",
    ChargeNotFound: "Recurso não encontrado durante a execução.",
    InvalidChargeItems: "Os dados de processamento são inválidos.",
    SettlementPaymentMismatch: "O recebimento não corresponde ao valor integral da cobrança.",
    InsufficientReceivedAmount: "A soma dos recebimentos ainda não cobre o valor da cobrança. Um novo recebimento permitirá o reprocessamento automático.",
    PaymentConnectionUnavailable: "A conexão bancária do recebimento está indisponível.",
    FinancialConnectionUnavailable: "A conexão financeira da cobrança está indisponível.",
    ProviderChanged: "O provedor financeiro da cobrança foi alterado.",
    ReceiptAccountMappingMissing: "Configure a conta de recebimento para esta conexão bancária.",
    ReceiptAccountMappingInvalid: "A conta de recebimento configurada é inválida.",
    ReceiptTimeZoneMissing: "Configure o fuso horário para registrar a data do recebimento.",
    ReceiptTimeZoneInvalid: "O fuso horário configurado para o recebimento é inválido.",
    InvalidSettlementCheckpoint: "O histórico da baixa diverge dos dados atuais da cobrança.",
    ChargeSettlementIndeterminate: "O sistema financeiro externo pode ter recebido a baixa; confira os recebimentos antes de tentar novamente.",
    ChargeSettlementPartial: "Parte dos lançamentos foi baixada; confira o resultado antes de reprocessar.",
    ChargeSettlementFailed: "O sistema financeiro externo não confirmou a baixa da cobrança.",
    JobHandlerError: "Ocorreu uma falha durante a execução.",
    InvalidHandlerResult: "O resultado da execução é inválido.",
  };
  return messages[code] ?? "A execução apresentou uma falha. Consulte o resultado disponível.";
}
