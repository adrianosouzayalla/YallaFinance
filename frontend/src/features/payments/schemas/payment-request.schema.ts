import { z } from "zod";
import { isValidDateOnly } from "@/utils/date";

export const paymentRequestSchema = z.object({
  paymentMethodIds: z.array(z.uuid().refine((id) => id !== "00000000-0000-0000-0000-000000000000", "Forma de pagamento inválida"))
    .min(1, "Selecione uma ou mais formas de pagamento")
    .refine((ids) => new Set(ids.map((id) => id.toLowerCase())).size === ids.length, "Não repita formas de pagamento"),
  expiresDate: z.string().min(1, "Informe a validade do link.").refine(isValidDateOnly, "Informe uma validade válida"),
  note: z.string().max(2000, "Use até 2.000 caracteres"),
});
export type PaymentRequestFormData = z.infer<typeof paymentRequestSchema>;
