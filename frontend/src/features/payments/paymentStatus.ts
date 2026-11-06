import { formatDate } from "@/utils/date";
import { paymentBlockLabel } from "./paymentReasons";
import { paymentDateOnly } from "./paymentDate";

export interface PaymentChargeStatusInput {
  eligible: boolean;
  blockReason: string | null;
  amount?: number;
  receivedAmount?: number;
  settled?: boolean;
  activePaymentRequest?: { expiresAt: string } | null;
  activity?: {
    latestRequest?: {
      status: string;
      createdAt?: string;
      expiresAt: string;
      expiredAt?: string | null;
      canceledAt?: string | null;
    } | null;
    latestAttempt?: {
      status: string;
      provider: string;
      paymentMethodName?: string;
      createdAt: string;
      updatedAt: string | null;
      expiresAt?: string | null;
      issuanceBlocked: boolean;
      failureMessage?: string | null;
    } | null;
    openRiskAttempt?: {
      status: string;
      provider: string;
      paymentMethodName?: string;
      createdAt: string;
      updatedAt: string | null;
      expiresAt?: string | null;
      issuanceBlocked: boolean;
      failureMessage?: string | null;
    } | null;
  } | null;
}

interface PaymentChargeStatus {
  label: string;
  detail: string;
  tone: "default" | "warning" | "info" | "success" | "error";
}

const date = (value: string | null | undefined) =>
  value ? formatDate(paymentDateOnly(value)) : "";

function nextPaymentStep(charge: PaymentChargeStatusInput, now: number): string {
  const request = charge.activity?.latestRequest;
  if (request?.status === "Active" && Date.parse(request.expiresAt) > now)
    return `Checkout ainda pode ser usado até ${date(request.expiresAt)}`;
  return paymentEligibilityDetail(charge);
}

export function paymentChargeStatus(
  charge: PaymentChargeStatusInput,
  now = Date.now(),
): PaymentChargeStatus {
  if (charge.settled)
    return {
      label: "Pago",
      detail: "Cobrança quitada após confirmação das baixas financeiras",
      tone: "success",
    };
  const received = charge.receivedAmount ?? 0;
  if (received > 0 && charge.amount !== undefined && received < charge.amount) {
    const money = (value: number) =>
      value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
    return {
      label: "Recebido parcialmente",
      detail: `${money(received)} recebido de ${money(charge.amount)} · saldo ${money(charge.amount - received)}`,
      tone: "info",
    };
  }
  if (received > 0 && charge.amount !== undefined && received >= charge.amount)
    return {
      label: "Recebimento confirmado",
      detail: "Aguardando confirmação das baixas no sistema financeiro externo",
      tone: "info",
    };
  const { activity } = charge;
  const attempt = activity?.openRiskAttempt ?? activity?.latestAttempt;
  if (attempt?.issuanceBlocked) {
    const updated = date(attempt.updatedAt);
    const route = [attempt.paymentMethodName, attempt.provider].filter(Boolean).join(" · ");
    if (attempt.status === "Pending")
      return {
        label: "Pendente de processamento",
        detail: `Tentativa registrada em ${date(attempt.createdAt)} · ${route}`,
        tone: "info",
      };
    if (attempt.status === "Processing")
      return {
        label: "Processando pagamento",
        detail: `Atualizado em ${updated} · ${route}`,
        tone: "info",
      };
    if (attempt.status === "Unknown")
      return {
        label: "Confirmação pendente",
        detail: `Verificação necessária · ${route}`,
        tone: "warning",
      };
    if (attempt.status === "AwaitingPayment")
      return {
        label: "Aguardando pagamento",
        detail: attempt.expiresAt
          ? `Instrumento válido até ${date(attempt.expiresAt)} · ${route}`
          : `Instrumento emitido · ${route}`,
        tone: "warning",
      };
    if (attempt.status === "Succeeded")
      return {
        label: "Recebimento confirmado",
        detail: `Confirmado em ${date(attempt.updatedAt)} · ${route}`,
        tone: "info",
      };
    return {
      label: "Revisão financeira necessária",
      detail: `A tentativa permanece bloqueando nova cobrança · ${route}`,
      tone: "error",
    };
  }

  const request = activity?.latestRequest;
  if (request?.status === "Active") {
    if (Date.parse(request.expiresAt) <= now)
      return {
        label: "Link vencido",
        detail: `Validade ${date(request.expiresAt)} · ${paymentEligibilityDetail(charge)}`,
        tone: "default",
      };
    return {
      label: "Aguardando pagamento",
      detail: `Link válido até ${date(request.expiresAt)}`,
      tone: "warning",
    };
  }

  if (attempt?.status === "Failed")
    return {
      label: "Falha no processamento",
      detail: `${attempt.failureMessage || `Atualizado em ${date(attempt.updatedAt)}`} · ${nextPaymentStep(charge, now)}`,
      tone: "error",
    };
  if (attempt?.status === "Expired")
    return {
      label: "Tentativa expirada",
      detail: `Expirada em ${date(attempt.expiresAt ?? attempt.updatedAt)} · ${nextPaymentStep(charge, now)}`,
      tone: "default",
    };
  if (attempt?.status === "Canceled")
    return {
      label: "Tentativa cancelada",
      detail: `Cancelada em ${date(attempt.updatedAt)} · ${nextPaymentStep(charge, now)}`,
      tone: "default",
    };

  if (request?.status === "Completed")
    return { label: "Pago", detail: "Solicitação concluída e cobrança quitada", tone: "success" };
  if (request?.status === "Canceled")
    return {
      label: "Cancelado",
      detail: `Solicitação cancelada em ${date(request.canceledAt ?? request.createdAt)}${charge.eligible ? " · disponível para nova solicitação" : ""}`,
      tone: "default",
    };
  if (request?.status === "Expired")
    return {
      label: "Link vencido",
      detail: `Validade ${date(request.expiresAt)} · ${paymentEligibilityDetail(charge)}`,
      tone: "default",
    };
  if (charge.activePaymentRequest && Date.parse(charge.activePaymentRequest.expiresAt) > now) {
    const expiry = request?.expiresAt ?? charge.activePaymentRequest?.expiresAt;
    return {
      label: "Aguardando pagamento",
      detail: expiry ? `Link válido até ${date(expiry)}` : "Link ativo",
      tone: "warning",
    };
  }
  if (charge.blockReason === "ActiveRequestExists")
    return {
      label: "Aguardando pagamento",
      detail:
        "Há uma solicitação ativa; a cobrança será liberada após pagamento, cancelamento ou vencimento do link",
      tone: "warning",
    };
  if (charge.blockReason === "OpenFinancialRisk")
    return {
      label: "Pagamento em andamento",
      detail: "Há um instrumento financeiro aberto; aguarde sua confirmação ou encerramento seguro",
      tone: "warning",
    };
  if (!charge.eligible)
    return {
      label: "Indisponível",
      detail: paymentBlockLabel(charge.blockReason),
      tone: "default",
    };
  return {
    label: "Disponível",
    detail: "Pode gerar uma nova solicitação de pagamento",
    tone: "success",
  };
}

export function paymentEligibilityDetail(
  charge: Pick<PaymentChargeStatusInput, "eligible" | "blockReason">,
): string {
  if (charge.eligible) return "Pode gerar uma nova solicitação";
  if (charge.blockReason === "ActiveRequestExists")
    return "Aguarda pagamento, cancelamento ou vencimento do link ativo";
  if (charge.blockReason === "OpenFinancialRisk")
    return "Aguarda confirmação ou encerramento seguro do instrumento financeiro";
  return paymentBlockLabel(charge.blockReason);
}

export function isPaymentRequestAwaitingPayment(
  status: string,
  expiresAt: string,
  now = Date.now(),
): boolean {
  return status === "Active" && Date.parse(expiresAt) > now;
}

export function paymentRequestLifecycleStatus(
  status: string,
  expiresAt?: string,
  now = Date.now(),
): string {
  if (status === "Active")
    return expiresAt && Date.parse(expiresAt) <= now ? "Link vencido" : "Aguardando pagamento";
  return (
    (
      { Expired: "Link vencido", Canceled: "Cancelado", Completed: "Pago" } as Record<
        string,
        string
      >
    )[status] ?? status
  );
}

export function paymentAttemptStatus(status: string): string {
  return (
    (
      {
        Pending: "Pendente de processamento",
        Processing: "Processando pagamento",
        AwaitingPayment: "Aguardando pagamento",
        Unknown: "Confirmação pendente",
        Succeeded: "Recebimento confirmado",
        Failed: "Falha no processamento",
        Expired: "Tentativa expirada",
        Canceled: "Tentativa cancelada",
      } as Record<string, string>
    )[status] ?? status
  );
}

export function paymentRequestChargeStatus(
  request: { id: string; status: string; createdAt: string; expiresAt: string },
  charge: PaymentChargeStatusInput,
  now = Date.now(),
): PaymentChargeStatus {
  if (charge.activity || charge.activePaymentRequest) return paymentChargeStatus(charge, now);
  const label = paymentRequestLifecycleStatus(request.status, request.expiresAt, now);
  const detail =
    request.status === "Active" && Date.parse(request.expiresAt) > now
      ? `Link válido até ${date(request.expiresAt)}`
      : request.status === "Completed"
        ? "Solicitação concluída e cobrança quitada"
        : request.status === "Canceled"
          ? `Solicitação cancelada em ${date(request.createdAt)}`
          : `Validade ${date(request.expiresAt)}`;
  const tone =
    label === "Pago" ? "success" : label === "Aguardando pagamento" ? "warning" : "default";
  return { label, detail, tone };
}
