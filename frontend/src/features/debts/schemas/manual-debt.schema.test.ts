import { describe, expect, it } from "vitest";
import { manualDebtSchema } from "./manual-debt.schema";

const valid = {
  customerId: "11111111-1111-4111-8111-111111111111",
  contractId: "22222222-2222-4222-8222-222222222222",
  categoryId: "33333333-3333-4333-8333-333333333333",
  amount: "123.45",
  occurrenceDate: "2020-01-01",
  dueDate: "2030-01-01",
  description: "Multa",
};
describe("manual debit", () => {
  it("accepts persisted GUIDs without an RFC version and rejects empty identifiers", () => {
    for (const field of ["customerId", "contractId", "categoryId"]) {
      expect(
        manualDebtSchema.safeParse({ ...valid, [field]: "a30e2e53-51a7-f55f-8832-d9147e06bcc5" })
          .success,
      ).toBe(true);
      expect(
        manualDebtSchema.safeParse({ ...valid, [field]: "00000000-0000-0000-0000-000000000000" })
          .success,
      ).toBe(false);
    }
  });
  it("requires due date and accepts historical/future dates", () => {
    expect(manualDebtSchema.safeParse(valid).success).toBe(true);
    expect(manualDebtSchema.safeParse({ ...valid, dueDate: "" }).success).toBe(false);
    expect(manualDebtSchema.safeParse({ ...valid, dueDate: "2026-02-30" }).success).toBe(false);
  });
  it("rejects missing links, blank description and invalid money", () => {
    for (const change of [
      { contractId: "" },
      { description: "   " },
      { amount: "0" },
      { amount: "1.001" },
    ])
      expect(manualDebtSchema.safeParse({ ...valid, ...change }).success).toBe(false);
  });
  it("preserves exact large decimals and rejects values beyond numeric(18,2)", () => {
    expect(manualDebtSchema.parse({ ...valid, amount: "90071992547409.91" }).amount).toBe(
      "90071992547409.91",
    );
    expect(manualDebtSchema.safeParse({ ...valid, amount: "9999999999999999.99" }).success).toBe(
      true,
    );
    expect(manualDebtSchema.safeParse({ ...valid, amount: "10000000000000000.00" }).success).toBe(
      false,
    );
  });
});
