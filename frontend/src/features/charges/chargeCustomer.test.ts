import { describe, expect, it, vi, beforeEach } from "vitest";
import { buildSelectedDebtDraft, createIdempotencyKey } from "@/features/charges/chargeFlow";
import { validateDebtSelection } from "@/features/charges/businessRules";
import { chargesApi } from "@/features/charges/api/charges.client";
import { request } from "@/services/api/api-client";
import type { Debt } from "@/features/debts/types/debt.types";
import type { ChargeDraft } from "@/features/charges/types/charge.types";

vi.mock("@/services/api/api-client", () => ({
  request: vi.fn(),
  ApiError: class extends Error {},
}));

const debt = {
  id: "debt-a",
  tenantId: "organization-a",
  customerId: "customer-a",
  tenantName: "Cliente A",
  categoryId: "category-a",
  category: "Multa",
  status: "Open",
  amount: 40,
  occurrenceDate: "2026-09-01",
  description: "Multa",
} as Debt & { customerId: string };
const payment = { method: "Pix" as const, dueDate: "2026-10-01", customerNote: "" };

describe("charge customer identity", () => {
  beforeEach(() => vi.resetAllMocks());

  it("rejects different customers within the same tenant", () => {
    expect(
      validateDebtSelection([debt, { ...debt, id: "debt-b", customerId: "customer-b" } as Debt])
        .valid,
    ).toBe(false);
  });

  it("rejects a selected debt already linked to a charge even if its status is Open", () => {
    expect(validateDebtSelection([{ ...debt, chargeId: "existing-charge" }]).valid).toBe(false);
    expect(() => buildSelectedDebtDraft([{ ...debt, chargeId: "existing-charge" }], payment))
      .toThrow("INVALID_DEBT_SELECTION");
  });

  it("rejects the same customer under a different tenant", () => {
    expect(
      validateDebtSelection([debt, { ...debt, id: "debt-b", tenantId: "organization-b" }]).valid,
    ).toBe(false);
  });

  it("builds the selected draft with a customer distinct from tenant", () => {
    const draft = buildSelectedDebtDraft([debt], payment);
    expect(draft).toMatchObject({ customerId: "customer-a", amount: 40 });
    expect(draft).not.toHaveProperty("tenantId");
  });

  it("sends customerId, never tenantId, and keeps both identities from the response", async () => {
    vi.mocked(request).mockImplementation(async (path) => {
      if (path.startsWith("/api/debts/")) return debt;
      if (path === "/api/payment-methods") return [{ id: "pix-a", code: "Pix" }];
      return {
        id: "charge-a",
        source: "Selected",
        tenantId: "organization-a",
        customerId: "customer-a",
        dueDate: payment.dueDate,
      };
    });
    const draft = buildSelectedDebtDraft([debt], payment);
    const result = await chargesApi.createCharge(draft, "key", { contextId: "session-test" });
    const call = vi.mocked(request).mock.calls.find(([path]) => path === "/api/charges")!;
    const payload = JSON.parse(call[1]!.body as string);
    expect(payload.customerId).toBe("customer-a");
    expect(payload).not.toHaveProperty("tenantId");
    expect(result).toMatchObject({ tenantId: "organization-a", customerId: "customer-a" });
  });

  it("uses the standalone customer in the idempotency key", () => {
    const first = {
      source: "standalone",
      customerId: "customer-a",
      standalone: {
        customerId: "customer-a",
        category: "Multa",
        categoryId: "category-a",
        paymentMethodId: "pix-a",
        amount: 10,
        occurrenceDate: "2026-09-01",
        descriptionDetail: "Multa",
        dueDate: payment.dueDate,
        customerNote: "",
      },
      movement: { id: "movement-a" },
    } as unknown as ChargeDraft;
    const second = { ...first, customerId: "customer-b" } as ChargeDraft;
    expect(createIdempotencyKey(first)).not.toBe(createIdempotencyKey(second));
  });
});
