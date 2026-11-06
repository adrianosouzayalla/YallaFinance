import { centsToDecimal, decimalToCents, formatExactCurrencyInput } from "@/utils/currency";
import { isValidDateOnly } from "@/utils/date";
import type { CreateDirectChargeRequest } from "@/services/api/charges/charge.types";
import { isValidPlate, normalizePlate } from "@/utils/plate";

export interface DirectChargeBasics {
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  document: string;
  contract: string;
  plate: string;
  costCenter: string;
  costCenterReference: string;
  dueDate: string;
  note: string;
}
export interface DirectDebtDraft {
  id: number;
  categoryId: string;
  category: string;
  occurrenceDate: string;
  amount: string;
  detail: string;
}
export function createDirectChargeBasics(): DirectChargeBasics {
  return {
    customerName: "",
    customerEmail: "",
    customerPhone: "",
    document: "",
    contract: "",
    plate: "",
    costCenter: "",
    costCenterReference: "",
    dueDate: "",
    note: "",
  };
}
export function createDirectDebtEditor(): Omit<DirectDebtDraft, "id"> {
  return { categoryId: "", category: "", occurrenceDate: "", amount: "", detail: "" };
}
export function validateDirectBasics(basics: DirectChargeBasics, customerFound: boolean): string {
  if (!customerFound) return "Consulte o cliente antes de continuar.";
  if (!basics.contract.trim()) return "Informe o contrato.";
  if (!isValidPlate(basics.plate))
    return "Informe uma placa válida, no formato ABC-1234 ou ABC1D23.";
  if (!basics.costCenterReference.trim() || !basics.costCenter.trim())
    return "Selecione o centro de custo.";
  if (!isValidDateOnly(basics.dueDate)) return "Informe um vencimento válido.";
  return "";
}
export function validateDirectDebt(debt: Omit<DirectDebtDraft, "id">): string {
  if (!debt.categoryId.trim() || !debt.category.trim()) return "Selecione uma categoria Yalla.";
  if (!isValidDateOnly(debt.occurrenceDate)) return "Informe uma ocorrência válida.";
  const cents = decimalToCents(debt.amount);
  if (!cents || BigInt(cents) <= BigInt(0))
    return "Informe um valor maior que zero, com até duas casas decimais.";
  if (!debt.detail.trim()) return "Informe a descrição.";
  return "";
}
export function composeDirectDebtDescription(
  debt: Pick<DirectDebtDraft, "category" | "detail">,
  contract: string,
): string {
  return `${debt.category.toLocaleUpperCase("pt-BR")} | ${debt.detail.trim()} | ${contract.trim()}`;
}
export function getDirectChargeTotal(debts: readonly Pick<DirectDebtDraft, "amount">[]): string {
  const total = debts.reduce(
    (sum, debt) => sum + BigInt(decimalToCents(debt.amount) || "0"),
    BigInt(0),
  );
  return centsToDecimal(total.toString())!;
}
export function formatDirectAmount(decimal: string): string {
  return `R$ ${formatExactCurrencyInput(decimalToCents(decimal))}`;
}
export function buildDirectChargeRequest(
  basics: DirectChargeBasics,
  debts: readonly DirectDebtDraft[],
): CreateDirectChargeRequest {
  return {
    customerName: basics.customerName.trim(),
    customerDocumentType: basics.document.replace(/\D/g, "").length === 11 ? "CPF" : "CNPJ",
    customerEmail: basics.customerEmail.trim() || null,
    customerPhone: basics.customerPhone.trim() || null,
    customerDocument: basics.document.replace(/\D/g, ""),
    contract: basics.contract.trim(),
    plate: normalizePlate(basics.plate),
    costCenterReference: basics.costCenterReference,
    costCenterName: basics.costCenter.trim(),
    dueDate: basics.dueDate,
    customerNote: basics.note,
    items: debts.map((debt) => {
      const cents = decimalToCents(debt.amount);
      if (!cents || BigInt(cents) <= BigInt(0))
        throw new Error("Informe um valor válido para o débito.");
      return {
        categoryId: debt.categoryId,
        categoryName: debt.category.trim(),
        occurrenceDate: debt.occurrenceDate,
        amount: centsToDecimal(cents)!,
        descriptionDetail: debt.detail.trim(),
      };
    }),
  };
}
