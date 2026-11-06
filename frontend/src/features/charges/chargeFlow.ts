import { validateDebtSelection } from "@/features/charges/businessRules";
import type { ChargeDraft, PaymentInput } from "@/services/api/charges/charge.types";
import type { Debt } from "@/services/api/debts/debt.types";

function roundCurrency(value: number): number {
  return Math.round(value * 100) / 100;
}

function stableValue(value: string): string {
  return encodeURIComponent(value.trim());
}

/** Builds the selected-debt draft only after the selection boundary is valid. */
export function buildSelectedDebtDraft(
  debts: Debt[],
  payment: PaymentInput,
): Extract<ChargeDraft, { source: "selected" }> {
  if (debts.length === 0) {
    throw new Error("EMPTY_DEBT_SELECTION");
  }

  const validation = validateDebtSelection(debts);

  if (!validation.valid) {
    throw new Error("INVALID_DEBT_SELECTION");
  }

  return {
    source: "selected",
    customerId: debts[0].customerId,
    customerName: debts[0].tenantName,
    debtIds: [...new Set(debts.map((debt) => debt.id))].sort(),
    amount: roundCurrency(debts.reduce((total, debt) => total + debt.amount, 0)),
    payment: { ...payment },
  };
}

/**
 * CODEX-INTEGRATION: a real payment endpoint must receive this stable request identifier as its idempotency key.
 */
export function createIdempotencyKey(draft: ChargeDraft): string {
  if (draft.source === "selected") {
    const debtIds = [...new Set(draft.debtIds)].sort().join(",");
    return [
      "selected",
      stableValue(draft.customerId),
      stableValue(debtIds),
      stableValue(roundCurrency(draft.amount).toFixed(2)),
      stableValue(draft.payment.paymentMethodId ?? ""),
      stableValue(draft.payment.dueDate),
      stableValue(draft.payment.customerNote),
    ].join(":");
  }

  return [
    "standalone",
    stableValue(draft.customerId),
    stableValue(draft.standalone.categoryId ?? ""),
    stableValue(draft.standalone.paymentMethodId ?? ""),
    stableValue(roundCurrency(draft.standalone.amount).toFixed(2)),
    stableValue(draft.standalone.occurrenceDate),
    stableValue(draft.standalone.descriptionDetail),
    stableValue(draft.standalone.dueDate),
    stableValue(draft.standalone.customerNote),
    stableValue(draft.movement.id),
  ].join(":");
}
