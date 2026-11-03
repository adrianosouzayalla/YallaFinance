import { validateDebtSelection } from "@/features/charges/businessRules";
import type { Debt } from "@/features/debts/types/debt.types";

export type SelectedDebtResolution = { ok: true; debts: Debt[] } | { ok: false; message: string };

const invalidSelectionMessage = "Selecione débitos em aberto de um único locatário";

export function buildReviewUrl(debts: Debt[]): string {
  const debtIds = [...new Set(debts.map((debt) => debt.id))].sort();
  return `/charges/review?source=selected&debtIds=${debtIds.join(",")}`;
}

export function resolveSelectedDebts(
  selectedDebtIds: string[],
  availableDebts: Debt[],
): SelectedDebtResolution {
  const debtIds = [...new Set(selectedDebtIds)];
  const debtById = new Map(availableDebts.map((debt) => [debt.id, debt]));
  const debts = debtIds.map((debtId) => debtById.get(debtId));

  if (debtIds.length === 0 || debts.some((debt) => debt === undefined)) {
    return { ok: false, message: "Não foi possível localizar os débitos selecionados" };
  }

  const resolvedDebts = debts as Debt[];
  const validation = validateDebtSelection(resolvedDebts);

  if (!validation.valid) {
    return { ok: false, message: invalidSelectionMessage };
  }

  return { ok: true, debts: resolvedDebts };
}
