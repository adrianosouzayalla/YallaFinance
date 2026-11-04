"use client";

import { useCallback, useEffect, useReducer, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  FormControl,
  FormControlLabel,
  Radio,
  RadioGroup,
  Stack,
  SvgIcon,
  Typography,
} from "@mui/material";
import type { SvgIconProps } from "@mui/material/SvgIcon";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import PixIcon from "@mui/icons-material/Pix";
import LockRoundedIcon from "@mui/icons-material/LockRounded";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import CreditCardRoundedIcon from "@mui/icons-material/CreditCardRounded";
import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import { LoadingState } from "@/components/ui/LoadingState";
import { ApiError } from "@/services/api/api-client";
import { publicPaymentApi } from "../../api/public-payment.client";
import type { PublicPaymentCheckout } from "../../types/payment.types";
import type { PublicCardPaymentInput } from "../../types/payment.types";
import { PixPaymentPanel } from "./PixPaymentPanel";
import { BoletoPaymentPanel } from "./BoletoPaymentPanel";
import { CreditCardPaymentForm } from "./CreditCardPaymentForm";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import { financeTokens } from "@/theme/designTokens";
import {
  checkoutFlowReducer,
  createInitialFlowState,
  deriveCheckoutView,
  expirationTimesOf,
  hasOpenAttempt,
  isPaymentReceived,
  isSameAttemptState,
  isSameCheckoutStatus,
  resolveStatusAlerts,
  unavailableMessage,
  type StatusAlertSpec,
} from "./checkoutFlow";
import { useCheckoutClock } from "./useCheckoutClock";
import { pollingIntervalFor, usePaymentStatusRefresh } from "./usePaymentStatusRefresh";
import { checkoutColors } from "./checkoutTheme";

export type PublicPaymentMethod = { code: string; name: string; instrumentType?: string | null };

type RefreshReason = "checkout" | "payment";

type Props = {
  token: string;
  checkout: PublicPaymentCheckout | null;
  temporaryError?: boolean;
};

const BRL = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

function formatExpiration(value: string) {
  const parsed = new Date(value);
  if (!Number.isFinite(parsed.getTime())) return "—";
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Sao_Paulo",
  }).format(parsed);
}

/** Wall-clock read for event handlers, where the time is needed for the current decision. */
function currentTimeMs() {
  return Date.now();
}

export function PublicPaymentCheckoutClient({ token, checkout: incomingCheckout, temporaryError = false }: Props) {
  const router = useRouter();
  const [lastCheckout, setLastCheckout] = useState(incomingCheckout);
  const checkout = incomingCheckout ?? lastCheckout;
  if (incomingCheckout && incomingCheckout !== lastCheckout) setLastCheckout(incomingCheckout);
  const [flow, dispatch] = useReducer(
    checkoutFlowReducer,
    checkout ?? null,
    (initialCheckout) => createInitialFlowState(initialCheckout, Date.now()),
  );
  const attempt = flow.attemptOverride ?? checkout?.attempt ?? null;
  const paymentReceived = isPaymentReceived(checkout ?? null, attempt);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [attemptError, setAttemptError] = useState("");
  const submitting = useRef(false);
  const cardCommandKey = useRef<string | null>(null);
  const [cardOutcomePending, setCardOutcomePending] = useState(false);
  const now = useCheckoutClock(expirationTimesOf(checkout ?? null, attempt));
  const [isRefreshing, startRefresh] = useTransition();
  const [refreshReason, setRefreshReason] = useState<RefreshReason>("checkout");

  // Background check: a light status request. The full checkout is reloaded only when the status changed,
  // and no new check starts while one is running or while a reload has not yet delivered new data.
  const statusCheckInFlight = useRef(false);
  const fullReloadPending = useRef(false);
  useEffect(() => {
    fullReloadPending.current = false;
  }, [incomingCheckout]);
  const checkPaymentStatus = useCallback(async () => {
    if (statusCheckInFlight.current || fullReloadPending.current) return;
    statusCheckInFlight.current = true;
    try {
      const status = await publicPaymentApi.getStatus(token);
      if (checkout && isSameCheckoutStatus(status, checkout)) return;
      fullReloadPending.current = true;
      router.refresh();
    } catch {
      // The data on screen stays; the next background tick tries again.
    } finally {
      statusCheckInFlight.current = false;
    }
  }, [token, checkout, router]);
  usePaymentStatusRefresh({
    refresh: checkPaymentStatus,
    refreshWhenVisible: !paymentReceived && Boolean(checkout),
    pollingIntervalMs: !paymentReceived && hasOpenAttempt(attempt, now)
      ? pollingIntervalFor(attempt?.instrumentType)
      : null,
  });

  // The server attempt is external input: when it differs from the one the flow observed, the flow
  // records it through its own reducer. The reducer ignores an attempt it has already observed.
  if (!isSameAttemptState(flow.observedServerAttempt, checkout?.attempt)) {
    dispatch({ type: "tentativa_do_servidor_mudou", serverAttempt: checkout?.attempt, now });
  }

  const stepHeadingRef = useRef<HTMLHeadingElement>(null);
  const previousStep = useRef(flow.step);
  useEffect(() => {
    if (previousStep.current === flow.step) return;
    previousStep.current = flow.step;
    stepHeadingRef.current?.focus();
  }, [flow.step]);

  if (!checkout) {
    const message = temporaryError
      ? "Não conseguimos abrir sua cobrança agora. Tente novamente em instantes."
      : "Este link de pagamento não está disponível. Entre em contato com quem enviou a cobrança.";
    return (
      <PageFrame>
        <Brand />
        <Alert
          severity={temporaryError ? "error" : "warning"}
          role="alert"
          action={temporaryError ? (
            <Button color="inherit" disabled={isRefreshing} onClick={refreshCheckout}>
              {isRefreshing ? "Tentando…" : "Tentar novamente"}
            </Button>
          ) : undefined}
        >
          {message}
        </Alert>
        <LoadingState mode="blocking" open={isRefreshing} message="Atualizando o checkout..." />
      </PageFrame>
    );
  }

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

  if (!paymentReceived && checkoutExpired) {
    return (
      <PageFrame>
        <Box component="main" sx={{ minHeight: "70vh", display: "grid", placeItems: "center" }}>
          <Typography role="alert" sx={{ color: checkoutColors.muted, fontSize: financeTokens.typography.checkoutBody, fontWeight: 600, textAlign: "center" }}>
            Este link de pagamento expirou. Peça um novo link a quem enviou a cobrança.
          </Typography>
        </Box>
      </PageFrame>
    );
  }

  async function startSelectedAttempt() {
    if (!checkout || !selectedMethod || !canStart || submitting.current) return;
    const reusingActiveCard = selectedCard && attemptHasOpenRisk
      && activeAttemptMethodCode.toLowerCase() === selectedCode.toLowerCase();
    if (selectedCard && !reusingActiveCard) {
      refreshCheckoutFor("payment");
      dispatch({ type: "cartao_iniciado" });
      return;
    }
    submitting.current = true;
    setIsSubmitting(true);
    setAttemptError("");

    try {
      const result = await publicPaymentApi.startAttempt(token, selectedMethod.code, createSecureUuidV4());
      const hasValidPix = result.status === "AwaitingPayment" && Boolean(result.brCode?.trim() && result.expiresAt)
        && new Date(result.expiresAt!).getTime() > currentTimeMs();
      dispatch({
        type: "tentativa_recebida",
        attempt: result,
        attemptMethodCode: result.paymentMethodCode ?? selectedMethod.code,
        step: selectedPix && !hasValidPix ? "method" : "payment",
      });
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) refreshCheckout();
      const isPaymentMethodUnavailable = error instanceof ApiError
        && error.status === 503
        && error.message !== "Falha na API";
      setAttemptError(isPaymentMethodUnavailable
        ? error.message
        : "Não conseguimos processar seu pagamento agora. Tente novamente em alguns instantes.");
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
      const result = await publicPaymentApi.startAttempt(token, selectedMethod.code, cardCommandKey.current, card);
      dispatch({ type: "tentativa_recebida", attempt: result, attemptMethodCode: selectedMethod.code, step: "payment" });
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
        setAttemptError("Não foi possível concluir este pagamento. Atualizamos a situação da sua cobrança.");
        refreshCheckoutFor("checkout");
        return;
      }
      // A lost response is not proof of rejection. Recover the persisted state without another card submission.
      setCardOutcomePending(true);
      dispatch({ type: "resposta_perdida_cartao" });
      setAttemptError("Estamos conferindo seu pagamento. Por favor, não pague novamente. Toque em “Atualizar situação” para ver o resultado.");
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
    dispatch({ type: "atualizacao_solicitada", serverAttempt: checkout?.attempt, now: currentTimeMs() });
    setCardOutcomePending(false);
    setAttemptError("");
    refreshCheckoutFor("checkout");
  }

  function changePaymentMethod() {
    dispatch({ type: "pagador_trocou_forma", serverAttempt: checkout?.attempt, now: currentTimeMs() });
    setAttemptError("");
  }

  return (
    <PageFrame>
      <Brand />

      <Box component="main" sx={{ minWidth: 0 }}>
        <Typography
          ref={stepHeadingRef}
          tabIndex={-1}
          variant="h1"
          component="h1"
          sx={{
            outline: "none",
            mb: 0.5,
            color: checkoutColors.ink,
            fontSize: financeTokens.typography.checkoutTitle,
            fontWeight: 750,
            letterSpacing: { xs: "-0.035em", md: "-0.045em" },
            lineHeight: 1.12,
          }}
        >
          {paymentReceived ? (partialPayment ? "Pagamento recebido parcialmente" : "Pagamento confirmado") : step === "method" ? "Escolha como pagar" : "Dados para pagamento"}
        </Typography>
        <Typography sx={{ color: checkoutColors.muted, mb: { xs: 2, md: 3 }, fontSize: financeTokens.typography.checkoutBody, lineHeight: 1.45 }}>
          {paymentReceived ? (partialPayment
            ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}; a confirmação da baixa está em andamento.`
            : "Recebemos seu pagamento. Não é necessário pagar novamente.") : step === "method"
            ? "Confira os dados da cobrança e selecione uma forma de pagamento."
            : "Use os dados abaixo para concluir o pagamento."}
        </Typography>

        {!paymentReceived && !linkAvailable && (
          <Alert severity="warning" role="alert" sx={{ mb: 2.5, borderRadius: 2 }}>
            {unavailableMessage(checkout, now)}
          </Alert>
        )}
        {!paymentReceived && linkAvailable && !hasMethods && (
          <Alert severity="warning" role="alert" sx={{ mb: 2.5, borderRadius: 2 }}>
            No momento não há formas de pagamento disponíveis para esta cobrança. Entre em contato com quem enviou a cobrança.
          </Alert>
        )}

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "minmax(0, 1fr) minmax(0, 1fr)" },
            gap: { xs: 2, md: 2.5 },
            alignItems: "stretch",
          }}
        >
          <Card elevation={0} sx={panelStyle}>
            <CardContent sx={{ p: { xs: 2.5, sm: 3.5, md: 4 }, display: "flex", flexDirection: "column", height: "100%" }}>
              <Typography component="h2" sx={sectionHeading}>Resumo da cobrança</Typography>

              <Stack spacing={{ xs: 2, md: 2.5 }} sx={{ mt: { xs: 2.5, md: 3.5 } }}>
                <Summary label="Contrato" value={checkout.contractNumber || "—"} />
                <Summary
                  label="Validade do link"
                  value={formatExpiration(checkout.expiresAt)}
                  divided
                />
              </Stack>

              <Box sx={{ borderTop: `1px solid ${checkoutColors.divider}`, mt: { xs: 2, md: 2.5 }, pt: { xs: 2, md: 2.5 } }}>
                <Typography sx={{ color: checkoutColors.muted, fontSize: financeTokens.typography.checkoutLabel, mb: 0.25 }}>Valor total</Typography>
                <Typography
                  aria-label={`Valor total ${BRL.format(checkout.amount)}`}
                  sx={{ color: checkoutColors.ink, fontSize: financeTokens.typography.checkoutAmount, fontWeight: 750, letterSpacing: "-0.04em", lineHeight: 1.2, overflowWrap: "anywhere" }}
                >
                  {BRL.format(checkout.amount)}
                </Typography>
              </Box>
            </CardContent>
          </Card>

          <Card elevation={0} sx={panelStyle}>
            <CardContent sx={{ p: { xs: 2.5, sm: 3.5, md: 4 }, display: "flex", flexDirection: "column", height: "100%" }}>
              {paymentReceived ? (
                <Stack role="status" aria-live="polite" spacing={2} sx={{ alignItems: "center", my: "auto", py: 4, textAlign: "center" }}>
                  <CheckCircleRoundedIcon sx={{ fontSize: 56, color: "success.main" }} aria-hidden="true" />
                  <Typography component="h2" sx={sectionHeading}>{partialPayment ? "Pagamento recebido parcialmente" : "Pagamento confirmado"}</Typography>
                  <Typography sx={{ color: checkoutColors.muted }}>{partialPayment
                    ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}. Ainda há saldo pendente.`
                    : paidAt ? `Pagamento realizado em ${formatExpiration(paidAt)}.` : "Pagamento realizado."}</Typography>
                </Stack>
              ) : step === "method" ? (
                <>
                  <Typography component="h2" sx={sectionHeading}>Formas de pagamento</Typography>
                  <Typography sx={{ color: checkoutColors.muted, mt: 0.5, mb: 2, fontSize: financeTokens.typography.body, lineHeight: 1.45 }}>
                    As opções exibidas foram autorizadas para esta cobrança.
                  </Typography>

                  <FormControl fullWidth>
                    {methods.length > 0 ? (
                      <RadioGroup
                        aria-label="Forma de pagamento"
                        value={selectedCode}
                        onChange={(event) => dispatch({ type: "pagador_escolheu_forma", methodCode: event.target.value })}
                        sx={{ gap: 1.5 }}
                      >
                        {methods.map((method) => (
                          <FormControlLabel
                            key={method.code}
                            value={method.code}
                            control={<Radio sx={{ ml: 1, color: checkoutColors.radioIdle, "&.Mui-checked": { color: checkoutColors.radioChecked } }} />}
                            label={<MethodCardContent method={method} />}
                            sx={optionSx}
                          />
                        ))}
                      </RadioGroup>
                    ) : null}
                  </FormControl>
                </>
              ) : (
                <Typography component="h2" sx={sectionHeading}>{(step === "card" || cardOutcomePending ? selectedMethod : attemptMethod)?.name ?? "Pagamento"}</Typography>
              )}

              {statusAlerts.map((alert) => <StatusAlert key={alert.message} {...alert} />)}

              {!paymentReceived && linkAvailable && step === "card" && selectedCard
                && (!attemptHasOpenRisk || selectedMethodDiffersFromActiveAttempt) && (
                <CreditCardPaymentForm missingCustomerFields={checkout.cardRequiredFields ?? []}
                  disabled={isSubmitting} onSubmit={submitCardAttempt} />
              )}
              {step === "payment" && attempt?.instrumentType === "CreditCard" && !paymentReceived && !attemptError && (
                <Alert severity={attempt.status === "Failed" ? "error" : "info"} sx={{ mt: 2 }}>{attempt.message ?? "Estamos conferindo seu pagamento com cartão. Por favor, não pague novamente."}</Alert>
              )}
              {cardOutcomePending && <Button onClick={refreshCheckout}>Atualizar situação</Button>}
              {step === "payment" && attemptPayable && attempt && (
                <PixPaymentPanel key={attempt.brCode} attempt={attempt} methodName={attemptMethod?.name} />
              )}
              {step === "payment" && boletoDetailsVisible && attempt?.boleto && (
                <BoletoPaymentPanel boleto={attempt.boleto} />
              )}

              {canAccessAttemptAction && hasMethods && (step !== "card" || selectedCard) && (
                <Box sx={{ mt: "auto", pt: 3, ...(attempt?.instrumentType === "Boleto" || step === "card" ? { borderTop: `1px solid ${checkoutColors.divider}` } : {}) }}>
                  {step === "card" ? (
                    <ChangeMethodButton onClick={changePaymentMethod} />
                  ) : step === "payment" && canChangeMethod && linkAvailable ? (
                    <ChangeMethodButton onClick={changePaymentMethod} />
                  ) : (
                    <Stack spacing={1.25}>
                      <PrimaryCheckoutButton disabled={!canStart} label={attemptActionLabel} onClick={() => void startSelectedAttempt()} />
                    </Stack>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>
      <LoadingState mode="blocking" open={isSubmitting || isRefreshing}
        message={isSubmitting ? "Finalizando pagamento..."
          : refreshReason === "payment" ? "Atualizando dados de pagamento..." : "Atualizando o checkout..."} />
    </PageFrame>
  );
}

const panelStyle = {
  minWidth: 0,
  minHeight: { md: 500 },
  border: `1px solid ${checkoutColors.panelBorder}`,
  borderRadius: "12px",
  bgcolor: checkoutColors.panelBackground,
  boxShadow: checkoutColors.panelShadow,
} as const;

const sectionHeading = {
  color: checkoutColors.ink,
  fontSize: financeTokens.typography.checkoutSection,
  fontWeight: 720,
  letterSpacing: "-0.025em",
  lineHeight: 1.3,
} as const;

const optionSx = {
  m: 0,
  minHeight: { xs: 84, md: 108 },
  border: `1px solid ${checkoutColors.divider}`,
  borderRadius: "14px",
  bgcolor: checkoutColors.white,
  alignItems: "center",
  transition: "border-color 120ms ease, background-color 120ms ease",
  "&:has(.Mui-checked)": { border: `2px solid ${checkoutColors.focus}`, bgcolor: checkoutColors.optionSelectedBackground },
  "&:focus-within": { outline: `3px solid ${checkoutColors.focusSoft}`, outlineOffset: 2 },
  "& .MuiFormControlLabel-label": { flex: 1, minWidth: 0 },
} as const;

const secondaryButtonSx = {
  minHeight: 48,
  borderRadius: "10px",
  borderColor: checkoutColors.secondaryBorder,
  color: checkoutColors.action,
  fontSize: financeTokens.typography.body,
  fontWeight: 700,
  textTransform: "none",
} as const;

const primaryButtonSx = {
  pt: 1.2,
  pb: 1.2,
  minHeight: 52,
  borderRadius: "10px",
  bgcolor: checkoutColors.action,
  boxShadow: checkoutColors.actionShadow,
  fontSize: financeTokens.typography.body,
  fontWeight: 700,
  textTransform: "none",
  "&:hover": { bgcolor: checkoutColors.actionHover, boxShadow: checkoutColors.actionShadowHover },
  "&:focus-visible": { outline: `3px solid ${checkoutColors.focus}`, outlineOffset: 3 },
  "&.Mui-disabled": { color: checkoutColors.white, bgcolor: checkoutColors.action, opacity: 0.72, boxShadow: "none", cursor: "not-allowed" },
} as const;

function PageFrame({ children }: { children: React.ReactNode }) {
  return (
    <Box sx={{ minHeight: financeTokens.shell.viewportHeight, bgcolor: checkoutColors.pageBackground, backgroundImage: checkoutColors.pageGradient, px: { xs: 1.5, sm: 2.5, md: 4 }, pt: { xs: 2, md: 4 }, pb: 2, overflowX: "hidden" }}>
      <Box sx={{ maxWidth: 1120, mx: "auto", minWidth: 0 }}>
        {children}
      </Box>
    </Box>
  );
}

function Brand() {
  return (
    <Stack component="header" direction="row" sx={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: { xs: 1, sm: 1.5 }, mb: { xs: 3, md: 4 } }}>
      <Box component="img" src="/assets/brand/yallafinance-logo.svg" alt="Yalla Finance" sx={{ display: "block", width: { xs: 150, sm: 190, md: 216 }, maxWidth: "100%", height: "auto" }} />
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", ml: "auto", flexShrink: 0, color: checkoutColors.muted }}>
        <LockRoundedIcon aria-hidden="true" sx={{ fontSize: { xs: 16, md: 20 } }} />
        <Typography sx={{ fontSize: financeTokens.typography.secondary, whiteSpace: "nowrap" }}>Pagamento seguro</Typography>
      </Stack>
    </Stack>
  );
}

function StatusAlert({ severity, role, message }: StatusAlertSpec) {
  return (
    <Alert severity={severity} role={role} sx={{ mt: 2.5, borderRadius: 2 }}>
      {message}
    </Alert>
  );
}

function ChangeMethodButton({ onClick }: { onClick: () => void }) {
  return (
    <Button fullWidth variant="outlined" size="large" onClick={onClick} sx={secondaryButtonSx}>
      Mudar Forma de Pagamento
    </Button>
  );
}

function PrimaryCheckoutButton({ disabled, label, onClick }: { disabled: boolean; label: string; onClick: () => void }) {
  return (
    <Button
      fullWidth
      variant="contained"
      size="large"
      disabled={disabled}
      onClick={onClick}
      endIcon={<ArrowForwardRoundedIcon />}
      sx={primaryButtonSx}
    >
      {label}
    </Button>
  );
}

function MethodCardContent({ method }: { method: PublicPaymentMethod }) {
  const isPix = method.instrumentType === "Pix";
  const Icon = isPix
    ? PixIcon
    : method.instrumentType === "CreditCard"
      ? CreditCardRoundedIcon
      : method.instrumentType === "Boleto"
        ? CheckoutBarcodeIcon
        : ReceiptLongOutlinedIcon;
  return (
    <Stack direction="row" spacing={{ xs: 1.25, md: 2 }} sx={{ alignItems: "center", minWidth: 0, py: 1.25 }}>
      <Box sx={{ display: "grid", placeItems: "center", flexShrink: 0, width: { xs: 36, md: 48 }, height: { xs: 36, md: 48 }, color: isPix ? checkoutColors.pixAccent : checkoutColors.ink }}>
        <Icon sx={{ fontSize: { xs: 28, md: 38 } }} aria-hidden="true" />
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography sx={{ color: checkoutColors.ink, fontSize: financeTokens.typography.checkoutSection, fontWeight: 700, lineHeight: 1.25, overflowWrap: "anywhere" }}>{method.name}</Typography>
      </Box>
    </Stack>
  );
}

function CheckoutBarcodeIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M2 5h2v14H2zm3 0h1v14H5zm3 0h3v14H8zm4 0h1v14h-1zm3 0h2v14h-2zm3 0h1v14h-1zm3 0h1v14h-1z" />
    </SvgIcon>
  );
}

function Summary({ label, value, divided = false }: { label: string; value: string; divided?: boolean }) {
  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      sx={{
        justifyContent: "space-between",
        alignItems: { xs: "flex-start", sm: "center" },
        minWidth: 0,
        ...(divided ? { borderTop: `1px solid ${checkoutColors.divider}`, pt: { xs: 2, md: 2.5 } } : {}),
      }}
    >
      <Typography sx={{ color: checkoutColors.muted, fontSize: financeTokens.typography.checkoutLabel, lineHeight: 1.4, flexShrink: 0 }}>{label}</Typography>
      <Typography sx={{ color: checkoutColors.ink, fontSize: financeTokens.typography.checkoutLabel, fontWeight: 550, textAlign: { xs: "left", sm: "right" }, minWidth: 0, overflowWrap: "anywhere" }}>{value}</Typography>
    </Stack>
  );
}
