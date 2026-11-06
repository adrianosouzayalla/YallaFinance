import type { z } from "zod";
import type { manualDebtSchema } from "@/services/api/debts/manualDebt.schema";
export type ManualDebtForm = z.infer<typeof manualDebtSchema>;
export type ManualDebtInput = ManualDebtForm & { idempotencyKey: string };
export interface ManualDebtReceipt {
  debtId: string;
}
