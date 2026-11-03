import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession as render } from "@/test/renderWithSession";
import { ChargeConfirmationClient } from "./ChargeConfirmationClient";
import { getChargeToday } from "../businessRules";
import { createIdempotencyKey } from "../chargeFlow";
import { ChargeIntegrationError } from "../charge-integration-error";
import type { Charge, ChargeDraft } from "../types/charge.types";

const mocks = vi.hoisted(() => ({
  create: vi.fn(),
  push: vi.fn(),
  replace: vi.fn(),
  toast: vi.fn(),
  draft: null as ChargeDraft | null,
}));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: mocks.push, replace: mocks.replace }),
}));
vi.mock("@/features/charges/context/ChargeDraftProvider", () => ({
  useChargeDraft: () => ({ draft: mocks.draft }),
}));
vi.mock("@/features/charges/api/charges.client", () => ({
  chargesApi: { createCharge: mocks.create },
}));
vi.mock("@/features/debts/api/debts.client", () => ({
  debtsApi: {
    getDebts: async () => (
      [
        {
          id: "debt-a",
          tenantId: "tenant-test",
          customerId: "customer-a",
          tenantName: "Cliente",
          categoryId: "fine",
          category: "Multa",
          amount: 42.25,
          status: "Open",
          occurrenceDate: "2026-09-01",
          description: "Multa",
        },
      ]
    ),
  },
}));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast: mocks.toast }) }));
const charge = (): Charge => ({
  id: "charge-a",
  reference: "10-123",
  tenantId: "tenant-test",
  customerId: "customer-a",
  source: "selected",
  idempotencyKey: "stable",
  categoryId: "fine",
  paymentMethodId: "pix",
  debtIds: ["debt-a"],
  amount: 42.25,
  payment: { method: "Pix", dueDate: getChargeToday(), customerNote: "" },
  providerExternalId: "receivable-a",
  pixId: "pix-a",
  pixPayload: "pix-payload",
  createdAt: "2026-10-01T12:00:00Z",
});
beforeEach(() => {
  vi.clearAllMocks();
  window.sessionStorage.clear();
  mocks.draft = {
    source: "selected",
    customerId: "customer-a",
    customerName: "Cliente",
    debtIds: ["debt-a"],
    amount: 42.25,
    payment: { method: "Pix", dueDate: getChargeToday(), customerNote: "" },
  };
});
afterEach(cleanup);

it("does not display a payment method, even for a legacy draft", () => {
  if (mocks.draft?.source !== "selected") throw new Error("Expected selected draft");
  mocks.draft.payment.paymentMethodId = "pix";
  render(<ChargeConfirmationClient />);
  expect(screen.queryByText("Forma de Pagamento")).not.toBeInTheDocument();
  expect(screen.queryByText("Pix cadastrado no banco")).not.toBeInTheDocument();
  expect(screen.getByText("Vencimento")).toBeVisible();
  expect(screen.queryByText(/padrão MVP/)).not.toBeInTheDocument();
});

it("sends once on a double click, keeps the idempotency key and navigates to the same success URL", async () => {
  let finish!: (value: Charge) => void;
  mocks.create.mockImplementation(
    () =>
      new Promise<Charge>((resolve) => {
        finish = resolve;
      }),
  );
  render(<ChargeConfirmationClient />);
  await screen.findByText("R$ 42,25");
  const button = await screen.findByRole("button", { name: "Gerar cobrança" });
  fireEvent.click(button);
  fireEvent.click(button);
  await waitFor(() => expect(mocks.create).toHaveBeenCalledTimes(1));
  expect(mocks.create).toHaveBeenCalledWith(mocks.draft, createIdempotencyKey(mocks.draft!), {
    contextId: "session-test",
  });
  finish(charge());
  await waitFor(() =>
    expect(mocks.push).toHaveBeenCalledWith("/charges/success?chargeId=charge-a"),
  );
});

it("keeps the created ID and rejection message without navigating or automatically resending", async () => {
  const partial = {
    ...charge(),
    financialIntegration: { outcome: "Rejected" as const, errorCode: "ReceivableConflict" },
  };
  mocks.create.mockRejectedValue(
    new ChargeIntegrationError(partial, "Integração rejeitada (ReceivableConflict)."),
  );
  const { client } = render(<ChargeConfirmationClient />);
  fireEvent.click(await screen.findByRole("button", { name: "Gerar cobrança" }));
  expect(await screen.findByText("Número da cobrança: 10-123")).toBeVisible();
  expect(mocks.push).not.toHaveBeenCalled();
  expect(mocks.create).toHaveBeenCalledTimes(1);
  expect(client.getQueryData(["charge", "tenant-test", "session-test", "charge-a"])).toMatchObject({
    id: "charge-a",
    financialIntegration: { outcome: "Rejected", errorCode: "ReceivableConflict" },
  });
  expect(window.sessionStorage.getItem("yalla-finance.charges.session-test")).toBeNull();
});

it("redirects a direct confirmation visit without a draft to the existing initial step", async () => {
  mocks.draft = null;
  render(<ChargeConfirmationClient />);
  await waitFor(() =>
    expect(mocks.replace).toHaveBeenCalledWith("/charges/new?feedback=revise-a-cobranca-primeiro"),
  );
  expect(mocks.create).not.toHaveBeenCalled();
});

it("keeps a completed charge in memory and navigates to success when browser storage is full", async () => {
  mocks.create.mockResolvedValue(charge());
  const original = Storage.prototype.setItem;
  const spy = vi.spyOn(Storage.prototype, "setItem").mockImplementation(function (
    this: Storage,
    key,
    value,
  ) {
    if (key.startsWith("yalla-finance.charges"))
      throw new DOMException("Storage full", "QuotaExceededError");
    return original.call(this, key, value);
  });
  try {
    render(<ChargeConfirmationClient />);
    fireEvent.click(await screen.findByRole("button", { name: "Gerar cobrança" }));
    await waitFor(() =>
      expect(mocks.push).toHaveBeenCalledWith("/charges/success?chargeId=charge-a"),
    );
    expect(mocks.create).toHaveBeenCalledTimes(1);
  } finally {
    spy.mockRestore();
  }
});
