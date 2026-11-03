import { manualDebtSchema } from "./schemas/manual-debt.schema";
import type { ManualDebtInput } from "./types/manual-debt.types";

export function readManualDebtAttempt(key: string): ManualDebtInput | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = sessionStorage.getItem(key);
    if (!raw) return null;
    const input = JSON.parse(raw) as ManualDebtInput;
    if (
      !manualDebtSchema.safeParse(input).success ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(input.idempotencyKey)
    )
      return null;
    return input;
  } catch {
    return null;
  }
}
export function storeManualDebtAttempt(key: string, input: ManualDebtInput | null) {
  if (input) sessionStorage.setItem(key, JSON.stringify(input));
  else sessionStorage.removeItem(key);
}
