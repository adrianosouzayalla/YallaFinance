"use client";

import { useEffect, useRef, useState, useTransition } from "react";
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
import type { PublicPaymentAttempt, PublicPaymentCheckout } from "../../types/payment.types";
import { PixPaymentPanel } from "./PixPaymentPanel";
import { BoletoPaymentPanel } from "./BoletoPaymentPanel";
import { CreditCardPaymentForm } from "./CreditCardPaymentForm";
import type { PublicCardPaymentInput } from "../../types/payment.types";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import { financeTokens } from "@/theme/designTokens";

export type PublicPaymentMethod = { code: string; name: string; instrumentType?: string | null };
type CheckoutStep = "method" | "card" | "payment";

type Props = {
  token: string;
  checkout: PublicPaymentCheckout | null;
  temporaryError?: boolean;
};

const NO_METHODS: PublicPaymentMethod[] = [];
const MAX_TIMEOUT_MS = 2_147_483_647;
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

function availability(checkout: PublicPaymentCheckout, now: number) {
  const expiresAt = new Date(checkout.expiresAt).getTime();
  return checkout.status.toLowerCase() === "active" && Number.isFinite(expiresAt) && expiresAt > now;
}

function hasExistingAttempt(attempt: PublicPaymentAttempt | null | undefined) {
  if (!attempt) return false;
  if (attempt.instrumentType?.toLowerCase() === "pix" && attempt.status === "AwaitingPayment") {
    return Boolean(attempt.brCode?.trim() && attempt.expiresAt
      && new Date(attempt.expiresAt).getTime() > Date.now());
  }
  if (attempt.instrumentType?.toLowerCase() === "pix" && attempt.status === "Unknown") return false;
  return ["AwaitingPayment", "Pending", "Processing", "Unknown"].includes(attempt.status);
}

function getAttemptSnapshot(attempt: PublicPaymentAttempt | null | undefined) {
  if (!attempt) return null;
  return JSON.stringify([
    attempt.status,
    attempt.expiresAt,
    attempt.brCode,
    attempt.location,
    attempt.canRetry,
    attempt.message,
    attempt.paymentMethodCode,
    attempt.instrumentType,
    attempt.boleto,
  ]);
}

function unavailableMessage(checkout: PublicPaymentCheckout, now: number) {
  switch (checkout.status.toLowerCase()) {
    case "completed":
      return "Esta cobrança já foi paga.";
    case "canceled":
      return "Esta solicitação foi cancelada.";
    case "expired":
      return "Este link de pagamento expirou.";
    case "active":
      return new Date(checkout.expiresAt).getTime() <= now
        ? "Este link de pagamento expirou."
        : "Este pagamento não está disponível no momento.";
    default:
      return "Este pagamento não está disponível no momento.";
  }
}

export function PublicPaymentCheckoutClient({ token, checkout: incomingCheckout, temporaryError = false }: Props) {
  const router = useRouter();
  const [lastCheckout, setLastCheckout] = useState(incomingCheckout);
  const checkout = incomingCheckout ?? lastCheckout;
  if (incomingCheckout && incomingCheckout !== lastCheckout) setLastCheckout(incomingCheckout);
  const methods = checkout?.methods ?? NO_METHODS;
  const requestStatus = checkout?.status;
  const requestExpiresAt = checkout?.expiresAt;
  const [selected, setSelected] = useState("");
  const [attemptMethodCode, setAttemptMethodCode] = useState(checkout?.attempt?.paymentMethodCode ?? "");
  const [step, setStep] = useState<CheckoutStep>(() => hasExistingAttempt(checkout?.attempt) ? "payment" : "method");
  const [attemptOverride, setAttemptOverride] = useState<PublicPaymentAttempt | null | undefined>(undefined);
  const checkoutAttemptSnapshot = getAttemptSnapshot(checkout?.attempt);
  const [attemptSource, setAttemptSource] = useState({ snapshot: checkoutAttemptSnapshot, override: attemptOverride });
  if (attemptSource.snapshot !== checkoutAttemptSnapshot || attemptSource.override !== attemptOverride) {
    if (attemptSource.snapshot !== checkoutAttemptSnapshot && checkout?.attempt?.instrumentType === "CreditCard")
      setAttemptOverride(undefined);
    setAttemptSource({ snapshot: checkoutAttemptSnapshot, override: attemptOverride });
    if (attemptOverride === undefined) {
      if (checkout?.attempt?.paymentMethodCode) {
        setAttemptMethodCode(checkout.attempt.paymentMethodCode);
      }
      setStep(hasExistingAttempt(checkout?.attempt) ? "payment" : "method");
    }
  }
  const attempt = attemptOverride ?? checkout?.attempt ?? null;
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [attemptError, setAttemptError] = useState("");
  const submitting = useRef(false);
  const cardCommandKey = useRef<string | null>(null);
  const [cardOutcomePending, setCardOutcomePending] = useState(false);
  const [currentTime, setCurrentTime] = useState(() => Date.now());
  const [isRefreshing, startRefresh] = useTransition();
  const paymentReceived = Boolean(checkout?.hasReceivedPayment)
    || checkout?.status.toLowerCase() === "completed" || attempt?.status === "Succeeded";
  const paymentSettled = Boolean(checkout?.settled);
  const receivedAmount = checkout?.receivedAmount ?? 0;
  const partialPayment = Boolean(checkout && paymentReceived && !paymentSettled
    && receivedAmount > 0 && receivedAmount < checkout.amount);
  const paidAt = checkout?.receivedAt ?? checkout?.settledAt;

  useEffect(() => {
    if (paymentReceived || !hasExistingAttempt(attempt)) return;
    const timer = window.setInterval(() => {
      if (document.visibilityState === "visible") router.refresh();
    }, attempt?.instrumentType === "CreditCard" || attempt?.instrumentType === "Boleto" ? 15_000 : 2_000);
    return () => window.clearInterval(timer);
  }, [paymentReceived, attempt, router]);

  useEffect(() => {
    const expiries = [
      requestStatus?.toLowerCase() === "active" && requestExpiresAt ? new Date(requestExpiresAt).getTime() : NaN,
      attempt?.status === "AwaitingPayment" && attempt.expiresAt ? new Date(attempt.expiresAt).getTime() : NaN,
    ].filter(Number.isFinite);
    if (expiries.length === 0) return;
    let timer: number | undefined;
    const checkExpiration = () => {
      const nextExpiry = expiries.filter((expiresAt) => expiresAt > currentTime).sort((a, b) => a - b)[0];
      if (nextExpiry === undefined) return;
      const remaining = nextExpiry - Date.now();
      timer = window.setTimeout(() => setCurrentTime(Date.now()), Math.min(remaining, MAX_TIMEOUT_MS));
    };
    checkExpiration();
    return () => { if (timer !== undefined) window.clearTimeout(timer); };
  }, [attempt?.expiresAt, attempt?.status, currentTime, requestExpiresAt, requestStatus]);

  if (!checkout) {
    const message = temporaryError
      ? "Não foi possível carregar o checkout. Tente novamente."
      : "Este link de pagamento está indisponível.";
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

  const checkoutExpired = checkout.status.toLowerCase() === "expired"
    || checkout.status.toLowerCase() === "active" && new Date(checkout.expiresAt).getTime() <= currentTime;
  if (!paymentReceived && checkoutExpired) {
    return (
      <PageFrame>
        <Box component="main" sx={{ minHeight: "70vh", display: "grid", placeItems: "center" }}>
          <Typography role="alert" sx={{ color: "#5b6d91", fontSize: financeTokens.typography.checkoutBody, fontWeight: 600, textAlign: "center" }}>
            Este link de pagamento expirou.
          </Typography>
        </Box>
      </PageFrame>
    );
  }

  const linkAvailable = availability(checkout, currentTime);
  const hasMethods = methods.length > 0;
  const selectedCode = methods.length === 1
    ? methods[0].code
    : methods.some((method) => method.code === selected) ? selected : "";
  const selectedMethod = methods.find((method) => method.code === selectedCode);
  const attemptMethod = methods.find((method) => method.code.toLowerCase()
    === (attempt?.paymentMethodCode || attemptMethodCode || selectedCode).toLowerCase());
  const selectedPix = selectedMethod?.instrumentType === "Pix";
  const selectedCard = selectedMethod?.instrumentType === "CreditCard";
  const selectedBoleto = selectedMethod?.instrumentType === "Boleto";
  const attemptExpired = attempt?.status === "Expired"
    || (attempt?.status === "AwaitingPayment" && Boolean(attempt.expiresAt)
      && new Date(attempt.expiresAt!).getTime() <= currentTime);
  const attemptPayable = linkAvailable && !paymentReceived && attempt?.status === "AwaitingPayment" && Boolean(attempt.brCode?.trim())
    && Boolean(attempt.expiresAt) && new Date(attempt.expiresAt!).getTime() > currentTime;
  const boletoDetailsAvailable = Boolean(attempt?.boleto?.identificationField?.trim()
    || attempt?.boleto?.barcode?.trim() || attempt?.boleto?.bankSlipUrl?.trim() || attempt?.boleto?.invoiceUrl?.trim());
  const boletoDetailsVisible = linkAvailable && !paymentReceived && attempt?.instrumentType === "Boleto"
    && (attempt.status === "AwaitingPayment" || attempt.status === "Unknown") && boletoDetailsAvailable;
  const attemptHasOpenRisk = attemptPayable
    || (attempt?.instrumentType === "Boleto" && attempt.status === "AwaitingPayment" && boletoDetailsAvailable)
    || (attempt?.instrumentType === "CreditCard" && attempt.status === "AwaitingPayment")
    || ["Pending", "Processing", "Unknown"].includes(attempt?.status ?? "")
    || (attempt?.status === "AwaitingPayment" && Boolean(attempt.expiresAt)
      && new Date(attempt.expiresAt!).getTime() > currentTime);
  const canChangeMethod = attemptHasOpenRisk || Boolean(attempt?.canRetry) || attemptExpired;
  const canAccessAttemptAction = linkAvailable && !paymentReceived && !cardOutcomePending;
  const activeAttemptMethodCode = attempt?.paymentMethodCode || attemptMethodCode;
  const selectedMethodDiffersFromActiveAttempt = attemptHasOpenRisk && Boolean(activeAttemptMethodCode)
    && activeAttemptMethodCode.toLowerCase() !== selectedCode.toLowerCase();
  const changingActiveMethod = step === "method" && selectedMethodDiffersFromActiveAttempt;
  const canStart = canAccessAttemptAction && hasMethods && Boolean(selectedMethod)
    && (selectedPix || selectedCard || selectedBoleto)
    && !isSubmitting;
  const pixNeedsRenewal = selectedPix && attempt?.instrumentType?.toLowerCase() === "pix"
    && (attempt.status === "Unknown" || (attempt.status === "AwaitingPayment" && !attemptPayable)
      || (attempt.status === "Failed" && attempt.canRetry) || attemptExpired);
  const attemptActionLabel = pixNeedsRenewal
    ? "Gerar Pix"
    : selectedCard ? "Continuar" : selectedBoleto ? "Gerar boleto" : "Finalizar pagamento";

  async function handleStartAttempt() {
    if (!checkout || !selectedMethod || !canStart || submitting.current) return;
    const reusingActiveCard = selectedCard && attemptHasOpenRisk
      && activeAttemptMethodCode.toLowerCase() === selectedCode.toLowerCase();
    if (selectedCard && !reusingActiveCard) { setStep("card"); return; }
    submitting.current = true;
    setIsSubmitting(true);
    setAttemptError("");

    try {
      const result = await publicPaymentApi.startAttempt(token, selectedMethod.code, createSecureUuidV4());
      setAttemptOverride(result);
      setAttemptMethodCode(result.paymentMethodCode ?? selectedMethod.code);
      const hasValidPix = result.status === "AwaitingPayment" && Boolean(result.brCode?.trim() && result.expiresAt)
        && new Date(result.expiresAt!).getTime() > Date.now();
      setStep(selectedPix && !hasValidPix ? "method" : "payment");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) refreshCheckout();
      const isPaymentMethodUnavailable = error instanceof ApiError
        && error.status === 503
        && error.message !== "Falha na API";
      setAttemptError(isPaymentMethodUnavailable
        ? error.message
        : "Não foi possível confirmar seu pagamento agora. Tente novamente em instantes.");
    } finally {
      submitting.current = false;
      setIsSubmitting(false);
    }
  }

  async function processCard(card: PublicCardPaymentInput) {
    if (!linkAvailable || !selectedMethod || submitting.current || cardOutcomePending) return;
    submitting.current = true;
    setIsSubmitting(true);
    setAttemptError("");
    cardCommandKey.current ??= createSecureUuidV4();
    try {
      const result = await publicPaymentApi.startAttempt(token, selectedMethod.code, cardCommandKey.current, card);
      setAttemptOverride(result);
      setAttemptMethodCode(selectedMethod.code);
      setStep("payment");
      cardCommandKey.current = null;
      if (result.status === "Succeeded") startRefresh(() => router.refresh());
    } catch (error) {
      if (error instanceof ApiError && (error.status === 400 || error.status === 422)) {
        cardCommandKey.current = null;
        setAttemptError("Confira os dados do cartão e do titular.");
        return;
      }
      // A lost response is not proof of rejection. Recover the persisted state without another card submission.
      setCardOutcomePending(true);
      setStep("payment");
      setAttemptError("Estamos verificando o pagamento. Atualize a situação antes de tentar novamente.");
      startRefresh(() => router.refresh());
    } finally {
      submitting.current = false;
      setIsSubmitting(false);
    }
  }

  function refreshCheckout() {
    setAttemptOverride(undefined);
    setCardOutcomePending(false);
    setAttemptError("");
    startRefresh(() => router.refresh());
  }


  function changePaymentMethod() {
    setSelected("");
    setAttemptOverride(undefined);
    setAttemptError("");
    setStep("method");
  }

  return (
    <PageFrame>
      <Brand />

      <Box component="main" sx={{ minWidth: 0 }}>
        <Typography
          variant="h1"
          component="h1"
          sx={{
            mb: 0.5,
            color: "#101d3b",
            fontSize: financeTokens.typography.checkoutTitle,
            fontWeight: 750,
            letterSpacing: { xs: "-0.035em", md: "-0.045em" },
            lineHeight: 1.12,
          }}
        >
          {paymentReceived ? (partialPayment ? "Pagamento recebido parcialmente" : "Pagamento confirmado") : step === "method" ? "Escolha como pagar" : "Dados para pagamento"}
        </Typography>
        <Typography sx={{ color: "#5b6d91", mb: { xs: 2, md: 3 }, fontSize: financeTokens.typography.checkoutBody, lineHeight: 1.45 }}>
          {paymentReceived ? (partialPayment
            ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}; a confirmação da baixa está em andamento.`
            : "Recebemos seu pagamento. Não é necessário pagar novamente.") : step === "method"
            ? "Confira os dados da cobrança e selecione uma forma de pagamento."
            : "Use os dados abaixo para concluir o pagamento."}
        </Typography>

        {!paymentReceived && !linkAvailable && (
          <Alert severity="warning" role="alert" sx={{ mb: 2.5, borderRadius: 2 }}>
            {unavailableMessage(checkout, currentTime)}
          </Alert>
        )}
        {!paymentReceived && linkAvailable && !hasMethods && (
          <Alert severity="warning" role="alert" sx={{ mb: 2.5, borderRadius: 2 }}>
            Nenhuma forma de pagamento está disponível. Entre em contato com a empresa responsável.
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

              <Box sx={{ borderTop: "1px solid #dce4ef", mt: { xs: 2, md: 2.5 }, pt: { xs: 2, md: 2.5 } }}>
                <Typography sx={{ color: "#5b6d91", fontSize: financeTokens.typography.checkoutLabel, mb: 0.25 }}>Valor total</Typography>
                <Typography
                  aria-label={`Valor total ${BRL.format(checkout.amount)}`}
                  sx={{ color: "#101d3b", fontSize: financeTokens.typography.checkoutAmount, fontWeight: 750, letterSpacing: "-0.04em", lineHeight: 1.2, overflowWrap: "anywhere" }}
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
                  <Typography sx={{ color: "#5b6d91" }}>{partialPayment
                    ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}. Ainda há saldo pendente.`
                    : paidAt ? `Pagamento realizado em ${formatExpiration(paidAt)}.` : "Pagamento realizado."}</Typography>
                </Stack>
              ) : step === "method" ? (
                <>
                  <Typography component="h2" sx={sectionHeading}>Formas de pagamento</Typography>
                  <Typography sx={{ color: "#5b6d91", mt: 0.5, mb: 2, fontSize: financeTokens.typography.body, lineHeight: 1.45 }}>
                    As opções exibidas foram autorizadas para esta cobrança.
                  </Typography>

                  <FormControl fullWidth>
                    {methods.length > 0 ? (
                      <RadioGroup
                        aria-label="Forma de pagamento"
                        value={selectedCode}
                        onChange={(event) => setSelected(event.target.value)}
                        sx={{ gap: 1.5 }}
                      >
                        {methods.map((method) => (
                          <FormControlLabel
                            key={method.code}
                            value={method.code}
                            control={<Radio sx={{ ml: 1, color: "#aebbd0", "&.Mui-checked": { color: "#1877f2" } }} />}
                            label={<MethodCardContent method={method} />}
                            sx={{
                              m: 0,
                              minHeight: { xs: 84, md: 108 },
                              border: "1px solid #dce4ef",
                              borderRadius: "14px",
                              bgcolor: "#fff",
                              alignItems: "center",
                              transition: "border-color 120ms ease, background-color 120ms ease",
                              "&:has(.Mui-checked)": { border: "2px solid #2385ff", bgcolor: "#f5faff" },
                              "&:focus-within": { outline: "3px solid rgba(35,133,255,.28)", outlineOffset: 2 },
                              "& .MuiFormControlLabel-label": { flex: 1, minWidth: 0 },
                            }}
                          />
                        ))}
                      </RadioGroup>
                    ) : null}
                  </FormControl>
                </>
              ) : (
                <Typography component="h2" sx={sectionHeading}>{(step === "card" || cardOutcomePending ? selectedMethod : attemptMethod)?.name ?? "Pagamento"}</Typography>
              )}

              {!paymentReceived && step === "method" && selectedMethod && !selectedPix && !selectedCard && !selectedBoleto && (
                <Alert severity="info" role="status" sx={{ mt: 2.5, borderRadius: 2 }}>
                  A forma selecionada ainda não está disponível para pagamento.
                </Alert>
              )}
              {!paymentReceived && !attemptError && attempt?.instrumentType !== "CreditCard" && (attempt?.status === "Processing" || attempt?.status === "Pending") && (
                <Alert severity="info" role="status" sx={{ mt: 2.5, borderRadius: 2 }}>
                  Estamos preparando seu pagamento. Aguarde um instante.
                </Alert>
              )}
              {!paymentReceived && !attemptError && attempt?.status === "Unknown" && attempt.instrumentType === "Boleto" && (
                step === "payment" ? (
                  <Alert severity="info" role="status" sx={{ mt: 2.5, borderRadius: 2 }}>
                    Estamos verificando a situação do seu boleto.
                  </Alert>
                ) : null
              )}
              {!paymentReceived && !attemptError && attempt?.status === "Unknown" && attempt.instrumentType === "Pix" && (
                step === "payment" ? (
                  <Alert severity="info" role="status" sx={{ mt: 2.5, borderRadius: 2 }}>
                    Estamos preparando seu Pix. Se necessário, você poderá gerar outro.
                  </Alert>
                ) : null
              )}
              {!paymentReceived && !attemptError && attemptOverride?.status === "Failed"
                && attemptOverride.instrumentType !== "CreditCard" && (
                <Alert severity="error" role="alert" sx={{ mt: 2.5, borderRadius: 2 }}>
                  {attemptOverride.message ?? "Não foi possível gerar o pagamento. Você pode tentar novamente."}
                </Alert>
              )}
              {!paymentReceived && !attemptError && attemptExpired && linkAvailable && (
                <Alert severity="info" role="status" sx={{ mt: 2.5, borderRadius: 2 }}>
                  Este código Pix expirou. Gere outro para continuar.
                </Alert>
              )}
              {!paymentReceived && attemptError
                && <Alert severity="error" role="alert" sx={{ mt: 2.5, borderRadius: 2 }}>{attemptError}</Alert>}

              {!paymentReceived && linkAvailable && step === "card" && selectedCard
                && (!attemptHasOpenRisk || selectedMethodDiffersFromActiveAttempt) && (
                <CreditCardPaymentForm missingCustomerFields={checkout.cardRequiredFields ?? []}
                  disabled={isSubmitting} onSubmit={processCard} />
              )}
              {step === "payment" && attempt?.instrumentType === "CreditCard" && !paymentReceived && !attemptError && (
                <Alert severity={attempt.status === "Failed" ? "error" : "info"} sx={{ mt: 2 }}>{attempt.message ?? "Estamos verificando seu pagamento com cartão. Não pague novamente."}</Alert>
              )}
              {cardOutcomePending && <Button onClick={refreshCheckout}>Atualizar situação</Button>}
              {step === "payment" && attemptPayable && attempt && (
                <PixPaymentPanel key={attempt.brCode} attempt={attempt} methodName={attemptMethod?.name} />
              )}
              {step === "payment" && boletoDetailsVisible && attempt?.boleto && (
                <BoletoPaymentPanel boleto={attempt.boleto} />
              )}

              {canAccessAttemptAction && hasMethods && (step !== "card" || selectedCard) && (
                <Box sx={{ mt: "auto", pt: 3, ...(attempt?.instrumentType === "Boleto" || step === "card" ? { borderTop: "1px solid #dce4ef" } : {}) }}>
                  {step === "card" ? (
                    <Button fullWidth variant="outlined" size="large" onClick={changePaymentMethod}
                      sx={{ minHeight: 48, borderRadius: "10px", borderColor: "#cbd7e7", color: "#073a8c", fontSize: financeTokens.typography.body, fontWeight: 700, textTransform: "none" }}>
                      Mudar Forma de Pagamento
                    </Button>
                  ) : step === "payment" && canChangeMethod && linkAvailable ? (
                    <Button fullWidth variant="outlined" size="large" onClick={changePaymentMethod}
                      sx={{ minHeight: 48, borderRadius: "10px", borderColor: "#cbd7e7", color: "#073a8c", fontSize: financeTokens.typography.body, fontWeight: 700, textTransform: "none" }}>
                      Mudar Forma de Pagamento
                    </Button>
                  ) : (
                    <Stack spacing={1.25}>
                      <Button
                        fullWidth
                        variant="contained"
                        size="large"
                        disabled={!canStart}
                        onClick={() => void handleStartAttempt()}
                        endIcon={<ArrowForwardRoundedIcon />}
                        sx={{
                          pt: 1.2,
                          pb: 1.2,
                          minHeight: 52,
                          borderRadius: "10px",
                          bgcolor: "#073a8c",
                          boxShadow: "0 3px 8px rgba(9, 48, 111, .12)",
                          fontSize: financeTokens.typography.body,
                          fontWeight: 700,
                          textTransform: "none",
                          "&:hover": { bgcolor: "#062f73", boxShadow: "0 4px 10px rgba(9, 48, 111, .18)" },
                          "&:focus-visible": { outline: "3px solid #2385ff", outlineOffset: 3 },
                          "&.Mui-disabled": { color: "#fff", bgcolor: "#073a8c", opacity: 0.72, boxShadow: "none", cursor: "not-allowed" },
                        }}
                      >
                        {attemptActionLabel}
                      </Button>
                    </Stack>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>
      <LoadingState mode="blocking" open={isSubmitting || isRefreshing}
        message={isSubmitting ? "Finalizando pagamento..." : "Atualizando o checkout..."} />
    </PageFrame>
  );
}

const panelStyle = {
  minWidth: 0,
  minHeight: { md: 500 },
  border: "1px solid #dfe6ef",
  borderRadius: "12px",
  bgcolor: "rgba(255,255,255,.92)",
  boxShadow: "0 2px 8px rgba(31, 56, 92, .025)",
} as const;

const sectionHeading = {
  color: "#101d3b",
  fontSize: financeTokens.typography.checkoutSection,
  fontWeight: 720,
  letterSpacing: "-0.025em",
  lineHeight: 1.3,
} as const;

function PageFrame({ children }: { children: React.ReactNode }) {
  return (
    <Box sx={{ minHeight: financeTokens.shell.viewportHeight, bgcolor: "#f8fafd", backgroundImage: "linear-gradient(115deg, #fbfcfe 0%, #f4f7fb 100%)", px: { xs: 1.5, sm: 2.5, md: 4 }, pt: { xs: 2, md: 4 }, pb: 2, overflowX: "hidden" }}>
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
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", ml: "auto", flexShrink: 0, color: "#5b6d91" }}>
        <LockRoundedIcon aria-hidden="true" sx={{ fontSize: { xs: 16, md: 20 } }} />
        <Typography sx={{ fontSize: financeTokens.typography.secondary, whiteSpace: "nowrap" }}>Pagamento seguro</Typography>
      </Stack>
    </Stack>
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
      <Box sx={{ display: "grid", placeItems: "center", flexShrink: 0, width: { xs: 36, md: 48 }, height: { xs: 36, md: 48 }, color: isPix ? "#10b9a5" : "#101d3b" }}>
        <Icon sx={{ fontSize: { xs: 28, md: 38 } }} aria-hidden="true" />
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography sx={{ color: "#101d3b", fontSize: financeTokens.typography.checkoutSection, fontWeight: 700, lineHeight: 1.25, overflowWrap: "anywhere" }}>{method.name}</Typography>
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
        ...(divided ? { borderTop: "1px solid #dce4ef", pt: { xs: 2, md: 2.5 } } : {}),
      }}
    >
      <Typography sx={{ color: "#5b6d91", fontSize: financeTokens.typography.checkoutLabel, lineHeight: 1.4, flexShrink: 0 }}>{label}</Typography>
      <Typography sx={{ color: "#101d3b", fontSize: financeTokens.typography.checkoutLabel, fontWeight: 550, textAlign: { xs: "left", sm: "right" }, minWidth: 0, overflowWrap: "anywhere" }}>{value}</Typography>
    </Stack>
  );
}
