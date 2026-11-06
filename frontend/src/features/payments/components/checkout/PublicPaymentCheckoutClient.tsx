"use client";

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
  Typography,
} from "@mui/material";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import { LoadingState } from "@/components/ui/LoadingState";
import type { PublicPaymentCheckout } from "@/services/api/payments/payment.types";
import { PixPaymentPanel } from "./PixPaymentPanel";
import { BoletoPaymentPanel } from "./BoletoPaymentPanel";
import { CreditCardPaymentForm } from "./CreditCardPaymentForm";
import { financeTokens } from "@/theme/designTokens";
import { unavailableMessage } from "./checkoutFlow";
import { checkoutColors } from "@/theme/checkoutTheme";
import { panelStyle, sectionHeading, optionSx } from "@/theme/checkoutStyles";
import { usePublicPaymentCheckout } from "./usePublicPaymentCheckout";
import { buildCheckoutScreen } from "./checkoutScreen";
import { Brand, CheckoutExpiredView, CheckoutUnavailableView, PageFrame } from "./checkoutViews";
import { BRL, formatExpiration } from "./checkoutFormatting";
import {
  ChangeMethodButton,
  MethodCardContent,
  PrimaryCheckoutButton,
  StatusAlert,
  Summary,
} from "./checkoutComponents";

type Props = {
  token: string;
  checkout: PublicPaymentCheckout | null;
  temporaryError?: boolean;
};

export function PublicPaymentCheckoutClient({
  token,
  checkout: incomingCheckout,
  temporaryError = false,
}: Props) {
  const state = usePublicPaymentCheckout({ token, checkout: incomingCheckout, temporaryError });
  const screen = buildCheckoutScreen(state);
  if (screen.kind === "unavailable")
    return (
      <CheckoutUnavailableView
        temporaryError={temporaryError}
        isRefreshing={screen.isRefreshing}
        onRetry={screen.refreshCheckout}
      />
    );
  if (screen.kind === "expired") return <CheckoutExpiredView />;
  const {
    attempt,
    attemptActionLabel,
    attemptError,
    attemptHasOpenRisk,
    attemptMethod,
    attemptPayable,
    boletoDetailsVisible,
    canAccessAttemptAction,
    canChangeMethod,
    canStart,
    cardOutcomePending,
    changePaymentMethod,
    checkout,
    dispatch,
    hasMethods,
    isRefreshing,
    isSubmitting,
    linkAvailable,
    methods,
    now,
    paidAt,
    partialPayment,
    paymentReceived,
    receivedAmount,
    refreshCheckout,
    refreshReason,
    selectedCard,
    selectedCode,
    selectedMethod,
    selectedMethodDiffersFromActiveAttempt,
    startSelectedAttempt,
    statusAlerts,
    step,
    stepHeadingRef,
    submitCardAttempt,
  } = screen;
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
          {paymentReceived
            ? partialPayment
              ? "Pagamento recebido parcialmente"
              : "Pagamento confirmado"
            : step === "method"
              ? "Escolha como pagar"
              : "Dados para pagamento"}
        </Typography>
        <Typography
          sx={{
            color: checkoutColors.muted,
            mb: { xs: 2, md: 3 },
            fontSize: financeTokens.typography.checkoutBody,
            lineHeight: 1.45,
          }}
        >
          {paymentReceived
            ? partialPayment
              ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}; a confirmação da baixa está em andamento.`
              : "Recebemos seu pagamento. Não é necessário pagar novamente."
            : step === "method"
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
            No momento não há formas de pagamento disponíveis para esta cobrança. Entre em contato
            com quem enviou a cobrança.
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
            <CardContent
              sx={{
                p: { xs: 2.5, sm: 3.5, md: 4 },
                display: "flex",
                flexDirection: "column",
                height: "100%",
              }}
            >
              <Typography component="h2" sx={sectionHeading}>
                Resumo da cobrança
              </Typography>

              <Stack spacing={{ xs: 2, md: 2.5 }} sx={{ mt: { xs: 2.5, md: 3.5 } }}>
                <Summary label="Contrato" value={checkout.contractNumber || "—"} />
                <Summary
                  label="Validade do link"
                  value={formatExpiration(checkout.expiresAt)}
                  divided
                />
              </Stack>

              <Box
                sx={{
                  borderTop: `1px solid ${checkoutColors.divider}`,
                  mt: { xs: 2, md: 2.5 },
                  pt: { xs: 2, md: 2.5 },
                }}
              >
                <Typography
                  sx={{
                    color: checkoutColors.muted,
                    fontSize: financeTokens.typography.checkoutLabel,
                    mb: 0.25,
                  }}
                >
                  Valor total
                </Typography>
                <Typography
                  aria-label={`Valor total ${BRL.format(checkout.amount)}`}
                  sx={{
                    color: checkoutColors.ink,
                    fontSize: financeTokens.typography.checkoutAmount,
                    fontWeight: 750,
                    letterSpacing: "-0.04em",
                    lineHeight: 1.2,
                    overflowWrap: "anywhere",
                  }}
                >
                  {BRL.format(checkout.amount)}
                </Typography>
              </Box>
            </CardContent>
          </Card>

          <Card elevation={0} sx={panelStyle}>
            <CardContent
              sx={{
                p: { xs: 2.5, sm: 3.5, md: 4 },
                display: "flex",
                flexDirection: "column",
                height: "100%",
              }}
            >
              {paymentReceived ? (
                <Stack
                  role="status"
                  aria-live="polite"
                  spacing={2}
                  sx={{ alignItems: "center", my: "auto", py: 4, textAlign: "center" }}
                >
                  <CheckCircleRoundedIcon
                    sx={{ fontSize: 56, color: "success.main" }}
                    aria-hidden="true"
                  />
                  <Typography component="h2" sx={sectionHeading}>
                    {partialPayment ? "Pagamento recebido parcialmente" : "Pagamento confirmado"}
                  </Typography>
                  <Typography sx={{ color: checkoutColors.muted }}>
                    {partialPayment
                      ? `Recebemos ${BRL.format(receivedAmount)} de ${BRL.format(checkout.amount)}. Ainda há saldo pendente.`
                      : paidAt
                        ? `Pagamento realizado em ${formatExpiration(paidAt)}.`
                        : "Pagamento realizado."}
                  </Typography>
                </Stack>
              ) : step === "method" ? (
                <>
                  <Typography component="h2" sx={sectionHeading}>
                    Formas de pagamento
                  </Typography>
                  <Typography
                    sx={{
                      color: checkoutColors.muted,
                      mt: 0.5,
                      mb: 2,
                      fontSize: financeTokens.typography.body,
                      lineHeight: 1.45,
                    }}
                  >
                    As opções exibidas foram autorizadas para esta cobrança.
                  </Typography>

                  <FormControl fullWidth>
                    {methods.length > 0 ? (
                      <RadioGroup
                        aria-label="Forma de pagamento"
                        value={selectedCode}
                        onChange={(event) =>
                          dispatch({
                            type: "pagador_escolheu_forma",
                            methodCode: event.target.value,
                          })
                        }
                        sx={{ gap: 1.5 }}
                      >
                        {methods.map((method) => (
                          <FormControlLabel
                            key={method.code}
                            value={method.code}
                            control={
                              <Radio
                                sx={{
                                  ml: 1,
                                  color: checkoutColors.radioIdle,
                                  "&.Mui-checked": { color: checkoutColors.radioChecked },
                                }}
                              />
                            }
                            label={<MethodCardContent method={method} />}
                            sx={optionSx}
                          />
                        ))}
                      </RadioGroup>
                    ) : null}
                  </FormControl>
                </>
              ) : (
                <Typography component="h2" sx={sectionHeading}>
                  {(step === "card" || cardOutcomePending ? selectedMethod : attemptMethod)?.name ??
                    "Pagamento"}
                </Typography>
              )}

              {statusAlerts.map((alert) => (
                <StatusAlert key={alert.message} {...alert} />
              ))}

              {!paymentReceived &&
                linkAvailable &&
                step === "card" &&
                selectedCard &&
                (!attemptHasOpenRisk || selectedMethodDiffersFromActiveAttempt) && (
                  <CreditCardPaymentForm
                    missingCustomerFields={checkout.cardRequiredFields ?? []}
                    disabled={isSubmitting}
                    onSubmit={submitCardAttempt}
                  />
                )}
              {step === "payment" &&
                attempt?.instrumentType === "CreditCard" &&
                !paymentReceived &&
                !attemptError && (
                  <Alert severity={attempt.status === "Failed" ? "error" : "info"} sx={{ mt: 2 }}>
                    {attempt.message ??
                      "Estamos conferindo seu pagamento com cartão. Por favor, não pague novamente."}
                  </Alert>
                )}
              {cardOutcomePending && <Button onClick={refreshCheckout}>Atualizar situação</Button>}
              {step === "payment" && attemptPayable && attempt && (
                <PixPaymentPanel
                  key={attempt.brCode}
                  attempt={attempt}
                  methodName={attemptMethod?.name}
                />
              )}
              {step === "payment" && boletoDetailsVisible && attempt?.boleto && (
                <BoletoPaymentPanel boleto={attempt.boleto} />
              )}

              {canAccessAttemptAction && hasMethods && (step !== "card" || selectedCard) && (
                <Box
                  sx={{
                    mt: "auto",
                    pt: 3,
                    ...(attempt?.instrumentType === "Boleto" || step === "card"
                      ? { borderTop: `1px solid ${checkoutColors.divider}` }
                      : {}),
                  }}
                >
                  {step === "card" ? (
                    <ChangeMethodButton onClick={changePaymentMethod} />
                  ) : step === "payment" && canChangeMethod && linkAvailable ? (
                    <ChangeMethodButton onClick={changePaymentMethod} />
                  ) : (
                    <Stack spacing={1.25}>
                      <PrimaryCheckoutButton
                        disabled={!canStart}
                        label={attemptActionLabel}
                        onClick={() => void startSelectedAttempt()}
                      />
                    </Stack>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>
      <LoadingState
        mode="blocking"
        open={isSubmitting || isRefreshing}
        message={
          isSubmitting
            ? "Finalizando pagamento..."
            : refreshReason === "payment"
              ? "Atualizando dados de pagamento..."
              : "Atualizando o checkout..."
        }
      />
    </PageFrame>
  );
}

export type { PublicPaymentMethod } from "./checkoutTypes";
