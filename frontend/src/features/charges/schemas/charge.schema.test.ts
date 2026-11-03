import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { standaloneChargeSchema, paymentSchema } from "./charge.schema";
import type { StandaloneChargeDraft } from "../types/charge.types";

beforeEach(() => {
  vi.useFakeTimers();
  vi.setSystemTime(new Date("2026-10-02T01:30:00Z"));
});
afterEach(() => vi.useRealTimers());
const valid: StandaloneChargeDraft = {
  customerId: "customer-a",
  category: "Multa",
  categoryId: "fine",
  paymentMethodId: "pix-a",
  amount: 1234.56,
  occurrenceDate: "2026-10-01",
  dueDate: "2026-10-01",
  descriptionDetail: "Multa",
  customerNote: "Nota",
  selectedMovementId: "movement-a",
};

it("preserves cents, selected IDs and today's date in São Paulo near UTC midnight", () => {
  expect(standaloneChargeSchema.parse(valid)).toEqual(valid);
  expect(
    paymentSchema.safeParse({ method: "Pix", dueDate: "2026-10-01", customerNote: "" }).success,
  ).toBe(true);
});

it("requires the description detail in the Zod field schema", () => {
  const result = standaloneChargeSchema.safeParse({ ...valid, descriptionDetail: "   " });

  expect(result.success).toBe(false);
  if (!result.success)
    expect(result.error.issues).toContainEqual(
      expect.objectContaining({ path: ["descriptionDetail"], code: "too_small" }),
    );
});

it.each([0, -1, NaN, Infinity])("preserves the message for an invalid amount %s", (amount) => {
  const result = standaloneChargeSchema.safeParse({ ...valid, amount });
  expect(result.success).toBe(false);
  if (!result.success)
    expect(result.error.issues).toContainEqual(
      expect.objectContaining({ path: ["amount"], message: "Informe um valor maior que zero" }),
    );
});

it("rejects future occurrences and past due dates with the existing messages", () => {
  const result = standaloneChargeSchema.safeParse({
    ...valid,
    occurrenceDate: "2026-10-02",
    dueDate: "2026-09-30",
  });
  expect(result.success).toBe(false);
  if (!result.success)
    expect(result.error.issues.map((issue) => issue.message)).toEqual([
      "A data da ocorrência não pode ser futura",
      "A data de vencimento não pode ser anterior a hoje",
    ]);
});
