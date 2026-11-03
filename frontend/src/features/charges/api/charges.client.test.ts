import { beforeEach, expect, it, vi } from "vitest";
import { chargesApi } from "./charges.client";
import { request } from "@/services/api/api-client";
import { ApiError } from "@/services/api/api-error";
import type { ChargeDraft } from "@/features/charges/types/charge.types";

vi.mock("@/services/api/api-client", () => ({
  request: vi.fn(),
}));

const payment = {
  method: "Pix" as const,
  paymentMethodId: "pix-b",
  dueDate: "2026-10-05",
  customerNote: "Nota",
};
const result = {
  id: "charge-a",
  reference: "10-9223372036854775807",
  source: "Selected",
  tenantId: "tenant-a",
  customerId: "customer-a",
  amount: 42.25,
  dueDate: payment.dueDate,
};

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(request).mockImplementation(async (url) => {
    if (url.startsWith("/api/debts/"))
      return { categoryId: "category-a", occurrenceDate: "2026-09-01", description: "Multa" };
    if (url === "/api/payment-methods") return [{ id: "pix-a" }];
    return result;
  });
});

it("creates a new charge without payment fields or payment catalog access", async () => {
  await chargesApi.createCharge({ source: "selected", customerId: "customer-a", customerName: "Cliente",
    debtIds: ["debt-a"], amount: 42.25, payment: { dueDate: "2026-10-20", customerNote: "" } }, "new-key", { contextId: "session-test" });
  const [, init] = vi.mocked(request).mock.calls.find(([url]) => url === "/api/charges")!;
  const body = JSON.parse(init!.body as string);
  expect(body).not.toHaveProperty("paymentMethod");
  expect(body).not.toHaveProperty("paymentMethodId");
  expect(body.dueDate).toBe("2026-10-20");
  expect(vi.mocked(request).mock.calls.some(([url]) => url === "/api/payment-methods")).toBe(false);
});

it("returns scheduled integration without treating the unconfirmed financial result as rejection", async () => {
  vi.mocked(request).mockImplementation(async (url) => {
    if (url === "/api/payment-methods") return [{ id: "pix-a" }];
    if (url.startsWith("/api/debts/")) return { categoryId: "category-a" };
    return {
      ...result,
      processing: { jobId: "job-a", status: "Pending" },
      financialIntegration: {
        outcome: "Indeterminate",
        errorCode: "IntegrationPendingOrUnconfirmed",
      },
    };
  });
  const draft: ChargeDraft = {
    source: "selected",
    customerId: "customer-a",
    customerName: "Cliente",
    debtIds: ["debt-a"],
    amount: 42.25,
    payment,
  };
  await expect(
    chargesApi.createCharge(draft, "key", { contextId: "session-test" }),
  ).resolves.toMatchObject({
    processing: { jobId: "job-a", status: "Pending" },
  });
});

it("preserves the selected debt payload and idempotency header without sending tenant authority", async () => {
  const draft: ChargeDraft = {
    source: "selected",
    customerId: "customer-a",
    customerName: "Cliente",
    debtIds: ["debt-a"],
    amount: 42.25,
    payment,
  };
  expect(
    await chargesApi.createCharge(draft, "stable-key", { contextId: "session-test" }),
  ).toMatchObject({
    id: result.id,
    reference: result.reference,
    tenantId: "tenant-a",
    customerId: "customer-a",
  });
  const posts = vi.mocked(request).mock.calls.filter(([url]) => url === "/api/charges");
  expect(posts).toHaveLength(1);
  expect(posts[0][1]).toMatchObject({
    method: "POST",
    headers: { "Idempotency-Key": "stable-key" },
  });
  expect(JSON.parse(posts[0][1]!.body as string)).toEqual({
    source: "selected",
    customerId: "customer-a",
    debtIds: ["debt-a"],
    amount: 42.25,
    categoryId: "category-a",
    occurrenceDate: "2026-09-01",
    descriptionDetail: "Multa",
    dueDate: payment.dueDate,
    customerNote: "Nota",
    selectedMovementId: null,
    paymentMethod: "Pix",
    paymentMethodId: "pix-b",
  });
});

it("preserves the standalone movement, category and selected payment", async () => {
  const draft: ChargeDraft = {
    source: "standalone",
    customerId: "customer-a",
    customerName: "Cliente",
    standalone: {
      customerId: "customer-a",
      category: "Multa",
      categoryId: "category-b",
      amount: 42.25,
      occurrenceDate: "2026-09-01",
      descriptionDetail: "Multa avulsa",
      dueDate: payment.dueDate,
      customerNote: "Nota",
      selectedMovementId: "movement-b",
      paymentMethodId: "pix-b",
    },
    movement: {
      id: "movement-b",
      customerId: "customer-a",
      tenantId: "tenant-a",
      occurrenceDate: "2026-09-01",
      plate: "ABC1D23",
      contract: "Contrato",
      product: "TRUST",
      fleet: "Frota",
      costCenter: "Centro",
    },
  };
  await chargesApi.createCharge(draft, "standalone-key", { contextId: "session-test" });
  const [, init] = vi.mocked(request).mock.calls.find(([url]) => url === "/api/charges")!;
  expect(JSON.parse(init!.body as string)).toEqual({
    source: "standalone",
    customerId: "customer-a",
    debtIds: [],
    amount: 42.25,
    categoryId: "category-b",
    occurrenceDate: "2026-09-01",
    descriptionDetail: "Multa avulsa",
    dueDate: payment.dueDate,
    customerNote: "Nota",
    selectedMovementId: "movement-b",
    paymentMethod: "Pix",
    paymentMethodId: "pix-b",
  });
});

it("does not repeat a financial POST after a transport failure", async () => {
  vi.mocked(request).mockImplementation(async (url) => {
    if (url.startsWith("/api/debts/")) return { categoryId: "category-a" };
    if (url === "/api/payment-methods") return [{ id: "pix-a" }];
    throw new Error("Connection lost");
  });
  const draft: ChargeDraft = {
    source: "selected",
    customerId: "customer-a",
    customerName: "Cliente",
    debtIds: ["debt-a"],
    amount: 42.25,
    payment,
  };
  await expect(
    chargesApi.createCharge(draft, "stable-key", { contextId: "session-test" }),
  ).rejects.toThrow("Connection lost");
  expect(vi.mocked(request).mock.calls.filter(([url]) => url === "/api/charges")).toHaveLength(1);
});

it.each(["Rejected", "Indeterminate"])(
  "preserves the created charge ID and %s integration outcome in the error",
  async (outcome) => {
    vi.mocked(request).mockImplementation(async (url) => {
      if (url === "/api/payment-methods") return [{ id: "pix-a" }];
      if (url.startsWith("/api/debts/")) return { categoryId: "category-a" };
      return { ...result, financialIntegration: { outcome, errorCode: "ReceivableConflict" } };
    });
    const draft: ChargeDraft = {
      source: "selected",
      customerId: "customer-a",
      customerName: "Cliente",
      debtIds: ["debt-a"],
      amount: 42.25,
      payment,
    };
    await expect(
      chargesApi.createCharge(draft, "stable-key", { contextId: "session-test" }),
    ).rejects.toMatchObject({
      charge: {
        id: "charge-a",
        financialIntegration: { outcome, errorCode: "ReceivableConflict" },
      },
    });
    expect(vi.mocked(request).mock.calls.filter(([url]) => url === "/api/charges")).toHaveLength(1);
  },
);

it("queries a charge by encoded ID with the session context", async () => {
  vi.mocked(request).mockResolvedValue({
    ...result,
    providerExternalId: "receivable-42",
    financialIntegration: { outcome: "Rejected", errorCode: "ReceivableConflict" },
  });
  const signal = new AbortController().signal;
  const charge = await chargesApi.getCharge("charge a/id", { contextId: "session-test", signal });
  expect(vi.mocked(request)).toHaveBeenCalledExactlyOnceWith("/api/charges/charge%20a%2Fid", {
    headers: { "X-Yalla-Context": "session-test" },
    signal,
  });
  expect(charge).toMatchObject({
    id: "charge-a",
    amount: 42.25,
    providerExternalId: "receivable-42",
    financialIntegration: { outcome: "Rejected", errorCode: "ReceivableConflict" },
  });
});

it("treats a charge not found in the tenant as absent and preserves transport failures", async () => {
  vi.mocked(request).mockRejectedValueOnce(new ApiError("NotFound", "Ausente", 404));
  expect(await chargesApi.getCharge("unknown", { contextId: "session-test" })).toBeNull();
  vi.mocked(request).mockRejectedValueOnce(new ApiError("API_UNAVAILABLE", "Indisponível", 503));
  await expect(
    chargesApi.getCharge("unknown", { contextId: "session-test" }),
  ).rejects.toMatchObject({ status: 503 });
});
