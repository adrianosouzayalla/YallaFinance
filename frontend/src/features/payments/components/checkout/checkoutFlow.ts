import type {
  PublicPaymentAttempt,
  PublicPaymentCheckout,
  PublicPaymentCheckoutStatus,
  PublicPaymentMethod,
} from "@/services/api/payments/payment.types";

type CheckoutStep = "method" | "card" | "payment";

export type CheckoutFlowState = {
  step: CheckoutStep;
  selectedMethodCode: string;
  attemptOverride: PublicPaymentAttempt | null | undefined;
  attemptMethodCode: string;
  methodChosenByPayer: boolean;
  /** Last server attempt the flow has observed; a different one means the server changed. */
  observedServerAttempt: PublicPaymentAttempt | null;
};

type CheckoutFlowEvent =
  | { type: "pagador_escolheu_forma"; methodCode: string }
  | {
      type: "pagador_trocou_forma";
      serverAttempt: PublicPaymentAttempt | null | undefined;
      now: number;
    }
  | { type: "cartao_iniciado" }
  | {
      type: "tentativa_recebida";
      attempt: PublicPaymentAttempt;
      attemptMethodCode: string;
      step: CheckoutStep;
    }
  | { type: "resposta_perdida_cartao" }
  | {
      type: "atualizacao_solicitada";
      serverAttempt: PublicPaymentAttempt | null | undefined;
      now: number;
    }
  | {
      type: "tentativa_do_servidor_mudou";
      serverAttempt: PublicPaymentAttempt | null | undefined;
      now: number;
    };

type CheckoutViewInput = {
  checkout: PublicPaymentCheckout;
  flow: CheckoutFlowState;
  cardOutcomePending: boolean;
  isSubmitting: boolean;
  now: number;
};

const NO_METHODS: PublicPaymentMethod[] = [];

export function hasOpenAttempt(
  attempt: PublicPaymentAttempt | null | undefined,
  now: number,
): boolean {
  if (!attempt) return false;
  if (attempt.instrumentType?.toLowerCase() === "pix") {
    // A Pix stays open only while its code is still valid; an unknown Pix must be generated again.
    if (attempt.status === "AwaitingPayment") {
      return Boolean(
        attempt.brCode?.trim() && attempt.expiresAt && new Date(attempt.expiresAt).getTime() > now,
      );
    }
    if (attempt.status === "Unknown") return false;
  }
  return ["AwaitingPayment", "Pending", "Processing", "Unknown"].includes(attempt.status);
}

/** Compares the fields that change what the payer sees; unrelated fields are ignored. */
export function isSameAttemptState(
  first: PublicPaymentAttempt | null | undefined,
  second: PublicPaymentAttempt | null | undefined,
): boolean {
  if (first === second) return true;
  if (!first || !second) return !first && !second;
  return (
    first.status === second.status &&
    (first.expiresAt ?? null) === (second.expiresAt ?? null) &&
    (first.brCode ?? null) === (second.brCode ?? null) &&
    (first.location ?? null) === (second.location ?? null) &&
    (first.canRetry ?? null) === (second.canRetry ?? null) &&
    (first.message ?? null) === (second.message ?? null) &&
    (first.paymentMethodCode ?? null) === (second.paymentMethodCode ?? null) &&
    (first.instrumentType ?? null) === (second.instrumentType ?? null) &&
    (first.boleto?.identificationField ?? null) === (second.boleto?.identificationField ?? null) &&
    (first.boleto?.barcode ?? null) === (second.boleto?.barcode ?? null) &&
    (first.boleto?.bankSlipUrl ?? null) === (second.boleto?.bankSlipUrl ?? null) &&
    (first.boleto?.invoiceUrl ?? null) === (second.boleto?.invoiceUrl ?? null)
  );
}

/** True when a status check shows nothing the loaded checkout has not already shown. */
export function isSameCheckoutStatus(
  status: PublicPaymentCheckoutStatus,
  checkout: PublicPaymentCheckout,
): boolean {
  return (
    status.status === checkout.status &&
    status.expiresAt === checkout.expiresAt &&
    Boolean(status.hasReceivedPayment) === Boolean(checkout.hasReceivedPayment) &&
    (status.receivedAmount ?? 0) === (checkout.receivedAmount ?? 0) &&
    (status.receivedAt ?? null) === (checkout.receivedAt ?? null) &&
    Boolean(status.settled) === Boolean(checkout.settled) &&
    (status.settledAt ?? null) === (checkout.settledAt ?? null) &&
    isSameAttemptState(status.attempt, checkout.attempt)
  );
}

export function isLinkAvailable(checkout: PublicPaymentCheckout, now: number): boolean {
  const expiresAt = new Date(checkout.expiresAt).getTime();
  return (
    checkout.status.toLowerCase() === "active" && Number.isFinite(expiresAt) && expiresAt > now
  );
}

export function isPaymentReceived(
  checkout: PublicPaymentCheckout | null,
  attempt: PublicPaymentAttempt | null | undefined,
): boolean {
  return (
    Boolean(checkout?.hasReceivedPayment) ||
    checkout?.status.toLowerCase() === "completed" ||
    attempt?.status === "Succeeded"
  );
}

/** Instants that can end the current view: the link itself and an awaiting attempt. */
export function expirationTimesOf(
  checkout: PublicPaymentCheckout | null,
  attempt: PublicPaymentAttempt | null | undefined,
): number[] {
  const times = [
    checkout?.status.toLowerCase() === "active" && checkout.expiresAt
      ? new Date(checkout.expiresAt).getTime()
      : NaN,
    attempt?.status === "AwaitingPayment" && attempt.expiresAt
      ? new Date(attempt.expiresAt).getTime()
      : NaN,
  ];
  return times.filter(Number.isFinite);
}

export function unavailableMessage(checkout: PublicPaymentCheckout, now: number): string {
  switch (checkout.status.toLowerCase()) {
    case "completed":
      return "Esta cobrança já foi paga. Você não precisa pagar novamente.";
    case "canceled":
      return "Esta cobrança foi cancelada. Entre em contato com quem enviou para mais informações.";
    case "expired":
      return "Este link de pagamento expirou. Peça um novo link a quem enviou a cobrança.";
    case "active":
      return new Date(checkout.expiresAt).getTime() <= now
        ? "Este link de pagamento expirou. Peça um novo link a quem enviou a cobrança."
        : "Este pagamento não está disponível no momento. Tente novamente mais tarde.";
    default:
      return "Este pagamento não está disponível no momento. Tente novamente mais tarde.";
  }
}

function resolveStepForAttempt(
  attempt: PublicPaymentAttempt | null | undefined,
  now: number,
): CheckoutStep {
  return hasOpenAttempt(attempt, now) ? "payment" : "method";
}

export function createInitialFlowState(
  checkout: PublicPaymentCheckout | null,
  now: number,
): CheckoutFlowState {
  return {
    step: resolveStepForAttempt(checkout?.attempt, now),
    selectedMethodCode: "",
    attemptOverride: undefined,
    attemptMethodCode: checkout?.attempt?.paymentMethodCode ?? "",
    methodChosenByPayer: false,
    observedServerAttempt: checkout?.attempt ?? null,
  };
}

/** Adopts the server attempt: records its method and, unless the payer chose a method, moves to its step. */
function adoptServerAttempt(
  state: CheckoutFlowState,
  serverAttempt: PublicPaymentAttempt | null | undefined,
  now: number,
): CheckoutFlowState {
  const serverMethodCode = serverAttempt?.paymentMethodCode;
  const withMethodCode = serverMethodCode
    ? { ...state, attemptMethodCode: serverMethodCode }
    : state;
  if (withMethodCode.methodChosenByPayer) return withMethodCode;
  return { ...withMethodCode, step: resolveStepForAttempt(serverAttempt, now) };
}

/** Releases a local attempt that was showing and falls back to the server attempt. */
function releaseAttemptOverride(
  state: CheckoutFlowState,
  serverAttempt: PublicPaymentAttempt | null | undefined,
  now: number,
): CheckoutFlowState {
  if (state.attemptOverride === undefined) return state;
  return adoptServerAttempt({ ...state, attemptOverride: undefined }, serverAttempt, now);
}

export function checkoutFlowReducer(
  state: CheckoutFlowState,
  event: CheckoutFlowEvent,
): CheckoutFlowState {
  switch (event.type) {
    case "pagador_escolheu_forma":
      return { ...state, selectedMethodCode: event.methodCode };
    case "pagador_trocou_forma":
      return releaseAttemptOverride(
        { ...state, selectedMethodCode: "", methodChosenByPayer: true, step: "method" },
        event.serverAttempt,
        event.now,
      );
    case "cartao_iniciado":
      return { ...state, methodChosenByPayer: true, step: "card" };
    case "tentativa_recebida":
      return {
        ...state,
        attemptOverride: event.attempt,
        attemptMethodCode: event.attemptMethodCode,
        methodChosenByPayer: false,
        step: event.step,
      };
    case "resposta_perdida_cartao":
      return { ...state, methodChosenByPayer: false, step: "payment" };
    case "atualizacao_solicitada":
      return releaseAttemptOverride(
        { ...state, methodChosenByPayer: false },
        event.serverAttempt,
        event.now,
      );
    case "tentativa_do_servidor_mudou": {
      if (isSameAttemptState(event.serverAttempt, state.observedServerAttempt)) return state;
      const observed = { ...state, observedServerAttempt: event.serverAttempt ?? null };
      if (event.serverAttempt?.instrumentType === "CreditCard") {
        return adoptServerAttempt(
          { ...observed, attemptOverride: undefined },
          event.serverAttempt,
          event.now,
        );
      }
      if (observed.attemptOverride !== undefined) return observed;
      return adoptServerAttempt(observed, event.serverAttempt, event.now);
    }
  }
}

export function deriveCheckoutView({
  checkout,
  flow,
  cardOutcomePending,
  isSubmitting,
  now,
}: CheckoutViewInput) {
  const methods = checkout.methods ?? NO_METHODS;
  const attempt = flow.attemptOverride ?? checkout.attempt ?? null;
  const paymentReceived = isPaymentReceived(checkout, attempt);
  const paymentSettled = Boolean(checkout.settled);
  const receivedAmount = checkout.receivedAmount ?? 0;
  const partialPayment =
    paymentReceived && !paymentSettled && receivedAmount > 0 && receivedAmount < checkout.amount;
  const paidAt = checkout.receivedAt ?? checkout.settledAt;
  const checkoutExpired =
    checkout.status.toLowerCase() === "expired" ||
    (checkout.status.toLowerCase() === "active" && new Date(checkout.expiresAt).getTime() <= now);
  const linkAvailable = isLinkAvailable(checkout, now);
  const hasMethods = methods.length > 0;
  const selectedCode =
    methods.length === 1
      ? methods[0].code
      : methods.some((method) => method.code === flow.selectedMethodCode)
        ? flow.selectedMethodCode
        : "";
  const selectedMethod = methods.find((method) => method.code === selectedCode);
  const attemptMethod = methods.find(
    (method) =>
      method.code.toLowerCase() ===
      (attempt?.paymentMethodCode || flow.attemptMethodCode || selectedCode).toLowerCase(),
  );
  const selectedPix = selectedMethod?.instrumentType === "Pix";
  const selectedCard = selectedMethod?.instrumentType === "CreditCard";
  const selectedBoleto = selectedMethod?.instrumentType === "Boleto";
  const attemptExpired =
    attempt?.status === "Expired" ||
    (attempt?.status === "AwaitingPayment" &&
      Boolean(attempt.expiresAt) &&
      new Date(attempt.expiresAt!).getTime() <= now);
  const attemptPayable =
    linkAvailable &&
    !paymentReceived &&
    attempt?.status === "AwaitingPayment" &&
    Boolean(attempt.brCode?.trim()) &&
    Boolean(attempt.expiresAt) &&
    new Date(attempt.expiresAt!).getTime() > now;
  const boletoDetailsAvailable = Boolean(
    attempt?.boleto?.identificationField?.trim() ||
    attempt?.boleto?.barcode?.trim() ||
    attempt?.boleto?.bankSlipUrl?.trim() ||
    attempt?.boleto?.invoiceUrl?.trim(),
  );
  const boletoDetailsVisible =
    linkAvailable &&
    !paymentReceived &&
    attempt?.instrumentType === "Boleto" &&
    (attempt.status === "AwaitingPayment" || attempt.status === "Unknown") &&
    boletoDetailsAvailable;
  const attemptHasOpenRisk =
    (attempt?.instrumentType === "Boleto" &&
      attempt.status === "AwaitingPayment" &&
      boletoDetailsAvailable) ||
    (attempt?.instrumentType === "CreditCard" && attempt.status === "AwaitingPayment") ||
    ["Pending", "Processing", "Unknown"].includes(attempt?.status ?? "") ||
    (attempt?.status === "AwaitingPayment" &&
      Boolean(attempt.expiresAt) &&
      new Date(attempt.expiresAt!).getTime() > now);
  const canChangeMethod = attemptHasOpenRisk || Boolean(attempt?.canRetry) || attemptExpired;
  const canAccessAttemptAction = linkAvailable && !paymentReceived && !cardOutcomePending;
  const activeAttemptMethodCode = attempt?.paymentMethodCode || flow.attemptMethodCode;
  const selectedMethodDiffersFromActiveAttempt =
    attemptHasOpenRisk &&
    Boolean(activeAttemptMethodCode) &&
    activeAttemptMethodCode.toLowerCase() !== selectedCode.toLowerCase();
  const canStart =
    canAccessAttemptAction &&
    hasMethods &&
    Boolean(selectedMethod) &&
    (selectedPix || selectedCard || selectedBoleto) &&
    !isSubmitting;
  const pixNeedsRenewal =
    selectedPix &&
    attempt?.instrumentType?.toLowerCase() === "pix" &&
    (attempt.status === "Unknown" ||
      (attempt.status === "AwaitingPayment" && !attemptPayable) ||
      (attempt.status === "Failed" && attempt.canRetry) ||
      attemptExpired);
  const attemptActionLabel = pixNeedsRenewal
    ? "Gerar Pix"
    : selectedCard
      ? "Continuar"
      : selectedBoleto
        ? "Gerar boleto"
        : "Finalizar pagamento";

  return {
    methods,
    attempt,
    paymentReceived,
    partialPayment,
    receivedAmount,
    paidAt,
    checkoutExpired,
    linkAvailable,
    hasMethods,
    selectedCode,
    selectedMethod,
    attemptMethod,
    selectedPix,
    selectedCard,
    selectedBoleto,
    attemptExpired,
    attemptPayable,
    boletoDetailsVisible,
    attemptHasOpenRisk,
    canChangeMethod,
    canAccessAttemptAction,
    activeAttemptMethodCode,
    selectedMethodDiffersFromActiveAttempt,
    canStart,
    pixNeedsRenewal,
    attemptActionLabel,
  };
}

export type StatusAlertSpec = {
  severity: "info" | "error";
  role: "status" | "alert";
  message: string;
};

export type StatusAlertInput = {
  paymentReceived: boolean;
  attemptError: string;
  step: CheckoutStep;
  selectedMethod: PublicPaymentMethod | undefined;
  selectedPix: boolean;
  selectedCard: boolean;
  selectedBoleto: boolean;
  attempt: PublicPaymentAttempt | null;
  attemptOverride: PublicPaymentAttempt | null | undefined;
  attemptExpired: boolean;
  linkAvailable: boolean;
};

/**
 * Status messages of the payment panel, in display order. Several can apply at once in rare
 * combinations, so the list is kept instead of picking one. A received payment hides them all.
 */
export function resolveStatusAlerts(input: StatusAlertInput): StatusAlertSpec[] {
  if (input.paymentReceived) return [];
  const { attempt, attemptOverride, attemptError, step } = input;
  const alerts: StatusAlertSpec[] = [];
  if (
    step === "method" &&
    input.selectedMethod &&
    !input.selectedPix &&
    !input.selectedCard &&
    !input.selectedBoleto
  ) {
    alerts.push({
      severity: "info",
      role: "status",
      message: "Esta forma de pagamento não está disponível agora. Escolha outra opção.",
    });
  }
  if (
    !attemptError &&
    attempt?.instrumentType !== "CreditCard" &&
    (attempt?.status === "Processing" || attempt?.status === "Pending")
  ) {
    alerts.push({
      severity: "info",
      role: "status",
      message: "Estamos preparando seu pagamento. Um instante, por favor.",
    });
  }
  if (
    !attemptError &&
    attempt?.status === "Unknown" &&
    attempt.instrumentType === "Boleto" &&
    step === "payment"
  ) {
    alerts.push({
      severity: "info",
      role: "status",
      message: "Estamos conferindo seu boleto. Isso pode levar alguns instantes.",
    });
  }
  if (
    !attemptError &&
    attempt?.status === "Unknown" &&
    attempt.instrumentType === "Pix" &&
    step === "payment"
  ) {
    alerts.push({
      severity: "info",
      role: "status",
      message: "Estamos preparando seu Pix. Se necessário, você poderá gerar outro.",
    });
  }
  if (
    !attemptError &&
    attemptOverride?.status === "Failed" &&
    attemptOverride.instrumentType !== "CreditCard"
  ) {
    alerts.push({
      severity: "error",
      role: "alert",
      message:
        attemptOverride.message ??
        "Não conseguimos gerar seu pagamento agora. Você pode tentar novamente.",
    });
  }
  if (!attemptError && input.attemptExpired && input.linkAvailable) {
    alerts.push({
      severity: "info",
      role: "status",
      message: "Este código Pix expirou. Toque em “Gerar Pix” para continuar.",
    });
  }
  if (attemptError) alerts.push({ severity: "error", role: "alert", message: attemptError });
  return alerts;
}
