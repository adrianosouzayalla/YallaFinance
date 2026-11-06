import type { RefreshReason, usePublicPaymentCheckout } from "./usePublicPaymentCheckout";
import { ApiError } from "@/services/api/apiClient";
import { publicPaymentApi } from "@/services/api/payments/publicPaymentApi.client";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import type { PublicCardPaymentInput } from "@/services/api/payments/payment.types";
import { currentTimeMs } from "./checkoutFormatting";
import { deriveCheckoutView, resolveStatusAlerts } from "./checkoutFlow";

type CheckoutState = ReturnType<typeof usePublicPaymentCheckout>;

export function buildCheckoutScreen(state: CheckoutState) {
  const {
    checkout,
    attempt,
    attemptError,
    cardCommandKey,
    cardOutcomePending,
    dispatch,
    flow,
    isSubmitting,
    now,
    paymentReceived,
    router,
    setAttemptError,
    setCardOutcomePending,
    setIsSubmitting,
    setRefreshReason,
    startRefresh,
    submitting,
    token,
    isRefreshing,
  } = state;
  if (!checkout) return { kind: "unavailable" as const, isRefreshing, refreshCheckout };
  const view = deriveCheckoutView({ checkout, flow, cardOutcomePending, isSubmitting, now });
  const {
    methods,
    hasMethods,
    linkAvailable,
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
    attemptActionLabel,
    partialPayment,
    paidAt,
    receivedAmount,
    checkoutExpired,
  } = view;
  const { step, attemptOverride } = flow;
  const statusAlerts = resolveStatusAlerts({
    paymentReceived,
    attemptError,
    step,
    selectedMethod,
    selectedPix,
    selectedCard,
    selectedBoleto,
    attempt,
    attemptOverride,
    attemptExpired,
    linkAvailable,
  });

  if (!paymentReceived && checkoutExpired) return { kind: "expired" as const };

  async function startSelectedAttempt() {
    if (!checkout || !selectedMethod || !canStart || submitting.current) return;
    const reusingActiveCard =
      selectedCard &&
      attemptHasOpenRisk &&
      activeAttemptMethodCode.toLowerCase() === selectedCode.toLowerCase();
    if (selectedCard && !reusingActiveCard) {
      refreshCheckoutFor("payment");
      dispatch({ type: "cartao_iniciado" });
      return;
    }
    submitting.current = true;
    setIsSubmitting(true);
    setAttemptError("");

    try {
      const result = await publicPaymentApi.startAttempt(
        token,
        selectedMethod.code,
        createSecureUuidV4(),
      );
      const hasValidPix =
        result.status === "AwaitingPayment" &&
        Boolean(result.brCode?.trim() && result.expiresAt) &&
        new Date(result.expiresAt!).getTime() > currentTimeMs();
      dispatch({
        type: "tentativa_recebida",
        attempt: result,
        attemptMethodCode: result.paymentMethodCode ?? selectedMethod.code,
        step: selectedPix && !hasValidPix ? "method" : "payment",
      });
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) refreshCheckout();
      const isPaymentMethodUnavailable =
        error instanceof ApiError && error.status === 503 && error.message !== "Falha na API";
      setAttemptError(
        isPaymentMethodUnavailable
          ? error.message
          : "Não conseguimos processar seu pagamento agora. Tente novamente em alguns instantes.",
      );
    } finally {
      submitting.current = false;
      setIsSubmitting(false);
    }
  }

  async function submitCardAttempt(card: PublicCardPaymentInput) {
    if (!linkAvailable || !selectedMethod || submitting.current || cardOutcomePending) return;
    submitting.current = true;
    setIsSubmitting(true);
    setAttemptError("");
    cardCommandKey.current ??= createSecureUuidV4();
    try {
      const result = await publicPaymentApi.startAttempt(
        token,
        selectedMethod.code,
        cardCommandKey.current,
        card,
      );
      dispatch({
        type: "tentativa_recebida",
        attempt: result,
        attemptMethodCode: selectedMethod.code,
        step: "payment",
      });
      cardCommandKey.current = null;
      if (result.status === "Succeeded") refreshCheckoutFor("checkout");
    } catch (error) {
      if (error instanceof ApiError && (error.status === 400 || error.status === 422)) {
        cardCommandKey.current = null;
        setAttemptError("Confira os dados do cartão e do titular.");
        return;
      }
      if (error instanceof ApiError && error.status === 409) {
        // A conflict is a server decision, not a lost response: the charge was paid or closed, so read it again.
        cardCommandKey.current = null;
        setAttemptError(
          "Não foi possível concluir este pagamento. Atualizamos a situação da sua cobrança.",
        );
        refreshCheckoutFor("checkout");
        return;
      }
      // A lost response is not proof of rejection. Recover the persisted state without another card submission.
      setCardOutcomePending(true);
      dispatch({ type: "resposta_perdida_cartao" });
      setAttemptError(
        "Estamos conferindo seu pagamento. Por favor, não pague novamente. Toque em “Atualizar situação” para ver o resultado.",
      );
      refreshCheckoutFor("checkout");
    } finally {
      submitting.current = false;
      setIsSubmitting(false);
    }
  }

  function refreshCheckoutFor(reason: RefreshReason) {
    setRefreshReason(reason);
    startRefresh(() => router.refresh());
  }

  function refreshCheckout() {
    dispatch({
      type: "atualizacao_solicitada",
      serverAttempt: checkout?.attempt,
      now: currentTimeMs(),
    });
    setCardOutcomePending(false);
    setAttemptError("");
    refreshCheckoutFor("checkout");
  }

  function changePaymentMethod() {
    dispatch({
      type: "pagador_trocou_forma",
      serverAttempt: checkout?.attempt,
      now: currentTimeMs(),
    });
    setAttemptError("");
  }

  return {
    kind: "ready" as const,
    ...state,
    checkout,
    attemptActionLabel,
    attemptHasOpenRisk,
    attemptMethod,
    attemptPayable,
    boletoDetailsVisible,
    canAccessAttemptAction,
    canChangeMethod,
    canStart,
    changePaymentMethod,
    hasMethods,
    linkAvailable,
    methods,
    paidAt,
    partialPayment,
    receivedAmount,
    refreshCheckout,
    selectedCard,
    selectedCode,
    selectedMethod,
    selectedMethodDiffersFromActiveAttempt,
    startSelectedAttempt,
    statusAlerts,
    step,
    submitCardAttempt,
  };
}
