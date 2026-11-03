import type { Debt, SelectionValidation } from "@/features/debts/types/debt.types";
import type { FieldErrors, StandaloneChargeDraft } from "@/features/charges/types/charge.types";

export function getChargeToday(): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Sao_Paulo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());
  const value = (type: string) => parts.find((part) => part.type === type)!.value;
  return `${value("year")}-${value("month")}-${value("day")}`;
}

export function validateChargeDueDate(
  dueDate: string,
  today = getChargeToday(),
): string | undefined {
  if (!dueDate.trim()) return "Informe o vencimento";
  if (dueDate < today) return "A data de vencimento não pode ser anterior a hoje";
  return undefined;
}

export function validateDebtSelection(debts: Debt[]): SelectionValidation {
  const tenantIds = new Set(debts.map((debt) => debt.tenantId));

  const customerIds = new Set(debts.map((debt) => debt.customerId));

  if (
    debts.length === 0 ||
    tenantIds.size !== 1 ||
    customerIds.size !== 1 ||
    debts.some((debt) => !debt.customerId)
  ) {
    return {
      valid: false,
      message: "Selecione débitos de um único locatário",
    };
  }

  if (debts.some((debt) => debt.status !== "Open" || Boolean(debt.chargeId))) {
    return {
      valid: false,
      message: "Apenas débitos em aberto podem ser cobrados",
    };
  }

  return { valid: true };
}

export function validateStandaloneDraft(draft: StandaloneChargeDraft, today: string): FieldErrors {
  const errors: FieldErrors = {};

  if (!draft.customerId) errors.customerId = "Selecione o locatário";
  if (!draft.categoryId) errors.categoryId = "Selecione a categoria";

  if (!Number.isFinite(draft.amount) || draft.amount <= 0) {
    errors.amount = "Informe um valor maior que zero";
  }

  if (!draft.occurrenceDate) {
    errors.occurrenceDate = "Informe a data da ocorrência";
  } else if (draft.occurrenceDate > today) {
    errors.occurrenceDate = "A data da ocorrência não pode ser futura";
  }

  if (draft.descriptionDetail.trim() === "") {
    errors.descriptionDetail = "Informe o detalhe da descrição";
  }

  const dueDateError = validateChargeDueDate(draft.dueDate, today);
  if (dueDateError) errors.dueDate = dueDateError;

  return errors;
}
