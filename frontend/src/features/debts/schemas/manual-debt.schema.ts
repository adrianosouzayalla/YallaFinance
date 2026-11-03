import { z } from "zod";
import { isValidDateOnly } from "@/utils/date";

const persistedId = (message: string) =>
  z
    .string()
    .regex(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i, message)
    .refine((value) => value !== "00000000-0000-0000-0000-000000000000", message);

export const manualDebtSchema = z.object({
  customerId: persistedId("Selecione um cliente"),
  contractId: persistedId("Selecione um contrato"),
  categoryId: persistedId("Selecione uma categoria"),
  amount: z.string().refine((value) => {
    if (!/^\d{1,16}(\.\d{1,2})?$/.test(value)) return false;
    const [whole, fraction = ""] = value.split(".");
    const cents = BigInt(whole + fraction.padEnd(2, "0"));
    return cents > BigInt(0) && cents <= BigInt("999999999999999999");
  }, "Informe um valor positivo com até duas casas decimais"),
  occurrenceDate: z.string().refine(isValidDateOnly, "Informe uma data de ocorrência válida"),
  dueDate: z.string().refine(isValidDateOnly, "Informe um vencimento válido"),
  description: z.string().refine((text) => text.trim().length > 0, "Informe a descrição"),
});
