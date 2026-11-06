import { z } from "zod";
import { getChargeToday, validateStandaloneDraft, validateChargeDueDate } from "../businessRules";
import type { FieldErrors, StandaloneChargeDraft } from "@/services/api/charges/charge.types";
import type { DebtCategory } from "@/services/api/debts/debt.types";

export function getStandaloneFieldErrors(draft: StandaloneChargeDraft): FieldErrors {
  const errors = validateStandaloneDraft(draft, getChargeToday());
  if (draft.customerId === "") errors.customerId = "Selecione o locatário";
  if (draft.occurrenceDate === "") errors.occurrenceDate = "Informe a data da ocorrência";
  return errors;
}

// Values are already normalized by the existing UI; preserve its financial rules and messages.
export const standaloneChargeSchema = z
  .object({
    customerId: z.string(),
    category: z.custom<DebtCategory | "">((value) => typeof value === "string"),
    categoryId: z.string().optional(),
    paymentMethodId: z.string().optional(),
    amount: z.custom<number>((value) => typeof value === "number", {
      message: "Informe um valor maior que zero",
    }),
    occurrenceDate: z.string(),
    descriptionDetail: z.string().trim().min(1, "Informe o detalhe da descrição"),
    dueDate: z.string(),
    customerNote: z.string(),
    selectedMovementId: z.string().optional(),
  })
  .superRefine((draft, context) => {
    for (const [field, message] of Object.entries(getStandaloneFieldErrors(draft))) {
      if (field === "descriptionDetail") continue;
      context.addIssue({ code: "custom", path: [field], message });
    }
  });

export const paymentSchema = z
  .object({
    method: z.literal("Pix").optional(),
    paymentMethodId: z.string().optional(),
    dueDate: z.string(),
    customerNote: z.string(),
  })
  .superRefine((payment, context) => {
    const message = validateChargeDueDate(payment.dueDate);
    if (message) context.addIssue({ code: "custom", path: ["dueDate"], message });
  });
