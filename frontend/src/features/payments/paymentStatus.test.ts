import { describe, expect, it } from "vitest";
import { paymentChargeStatus, paymentRequestLifecycleStatus } from "./paymentStatus";

describe("paymentChargeStatus", () => {
  it("shows an active request as awaiting payment and explains its expiry", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "ActiveRequestExists",
      activity: { latestRequest: { status: "Active", expiresAt: "2026-10-11T02:59:59Z" } } }, Date.parse("2026-10-09T12:00:00Z")))
      .toEqual({ label: "Aguardando pagamento", detail: "Link válido até 10/10/2026", tone: "warning" });
  });

  it("prioritizes an active request over an expired attempt from an older request", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "ActiveRequestExists",
      activity: {
        latestRequest: { status: "Active", expiresAt: "2026-10-11T02:59:59Z" },
        latestAttempt: { status: "Expired", provider: "TestPixProvider", createdAt: "2026-10-08T10:00:00Z",
          updatedAt: "2026-10-09T10:00:00Z", issuanceBlocked: false },
      } }, Date.parse("2026-10-09T12:00:00Z")))
      .toMatchObject({ label: "Aguardando pagamento", tone: "warning" });
  });

  it("shows an expired request as available for a new request", () => {
    expect(paymentChargeStatus({ eligible: true, blockReason: null,
      activity: { latestRequest: { status: "Active", expiresAt: "2026-10-09T10:00:00Z" } } }, Date.parse("2026-10-09T12:00:00Z")))
      .toEqual({ label: "Link vencido", detail: "Validade 09/10/2026 · Pode gerar uma nova solicitação", tone: "default" });
  });

  it("keeps the active request as the displayed status after a failed attempt", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "ActiveRequestExists",
      activity: {
        latestRequest: { status: "Active", expiresAt: "2026-10-11T02:59:59Z" },
        latestAttempt: { status: "Failed", provider: "TestPixProvider", createdAt: "2026-10-09T10:00:00Z", updatedAt: "2026-10-09T11:00:00Z", issuanceBlocked: false },
      } }, Date.parse("2026-10-09T12:00:00Z")))
      .toMatchObject({ label: "Aguardando pagamento", tone: "warning" });
  });

  it("distinguishes processing and confirmation from an open Pix", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "OpenFinancialRisk",
      activity: { latestAttempt: { status: "Processing", provider: "TestPixProvider", createdAt: "2026-10-09T11:00:00Z", updatedAt: "2026-10-09T12:00:00Z", issuanceBlocked: true } } }))
      .toMatchObject({ label: "Processando pagamento", tone: "info" });
    expect(paymentChargeStatus({ eligible: false, blockReason: "OpenFinancialRisk",
      activity: { latestAttempt: { status: "AwaitingPayment", provider: "TestPixProvider", createdAt: "2026-10-09T11:00:00Z", updatedAt: "2026-10-09T12:00:00Z", issuanceBlocked: true, expiresAt: "2026-10-11T02:59:59Z" } } }))
      .toMatchObject({ label: "Aguardando pagamento", tone: "warning" });
  });

  it("keeps an expired link blocked while an emitted Pix remains payable", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "OpenFinancialRisk",
      activity: {
        latestRequest: { status: "Expired", expiresAt: "2026-10-09T10:00:00Z" },
        latestAttempt: { status: "Failed", provider: "TestPixProvider", createdAt: "2026-10-09T10:00:00Z", updatedAt: "2026-10-09T11:00:00Z", issuanceBlocked: false },
        openRiskAttempt: { status: "AwaitingPayment", provider: "TestPixProvider", paymentMethodName: "Pix", createdAt: "2026-10-09T09:00:00Z", updatedAt: "2026-10-09T10:00:00Z", expiresAt: "2026-10-10T02:59:59Z", issuanceBlocked: true },
      } }, Date.parse("2026-10-09T12:00:00Z")))
      .toMatchObject({ label: "Aguardando pagamento", detail: "Instrumento válido até 09/10/2026 · Pix · TestPixProvider" });
  });

  it("does not infer a paid state from removed receipt records", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "ReceiptAlreadyRecorded" }))
      .toEqual({ label: "Indisponível", detail: "Indisponível", tone: "default" });
  });

  it("uses provider-neutral wording while a received charge awaits financial settlement", () => {
    expect(paymentChargeStatus({ eligible: false, blockReason: "OpenFinancialRisk", amount: 100, receivedAmount: 100 }))
      .toMatchObject({ label: "Recebimento confirmado", detail: "Aguardando confirmação das baixas no sistema financeiro externo" });
  });

  it("uses operational lifecycle labels for generated requests", () => {
    expect(paymentRequestLifecycleStatus("Active", "2026-10-11T02:59:59Z", Date.parse("2026-10-09T12:00:00Z")))
      .toBe("Aguardando pagamento");
    expect(paymentRequestLifecycleStatus("Completed")).toBe("Pago");
    expect(paymentRequestLifecycleStatus("Canceled")).toBe("Cancelado");
  });
});
