import { cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession as render } from "@/test/renderWithSession";
import { ChargeSuccessClient } from "./ChargeSuccessClient";
import { ApiError } from "@/services/api/api-error";
import type { Charge } from "../types/charge.types";
import ChargesLayout from "@/app/(finance)/charges/layout";
import ChargeSuccessPage from "@/app/(finance)/charges/success/page";

const mocks = vi.hoisted(() => ({
  debts: vi.fn(),
  charge: vi.fn(),
  clearDraft: vi.fn(),
  payment: {
    eligible: true,
    blockReason: null,
  } as import("@/features/payments/types/payment.types").PaymentCharge,
}));
vi.mock("@/features/payments/hooks/usePaymentPreparation", () => ({
  usePaymentPreparation: (ids: string[]) => ({
    data: { charges: ids.map((id) => ({ ...mocks.payment, id })) },
    isFetching: false,
    isError: false,
  }),
}));
vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams("chargeId=charge-a"),
}));
vi.mock("@/features/charges/context/ChargeDraftProvider", () => ({
  ChargeDraftProvider: ({ children }: React.PropsWithChildren) => children,
  useChargeDraft: () => ({ clearChargeIntent: mocks.clearDraft }),
}));
vi.mock("@/features/debts/api/debts.client", () => ({ debtsApi: { listDebts: mocks.debts } }));
vi.mock("@/features/charges/api/charges.client", () => ({
  chargesApi: { getCharge: mocks.charge },
}));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: async () => [{ id: "fine", name: "Multa" }] },
}));
vi.mock("@/features/catalogs/api/payment-methods.client", () => ({
  paymentMethodsApi: { listPaymentMethods: async () => [{ id: "pix", name: "PIX B" }] },
}));
const charge: Charge = {
  id: "charge-a",
  reference: "10-123",
  tenantId: "tenant-test",
  customerId: "customer-a",
  source: "standalone",
  idempotencyKey: "stable",
  categoryId: "fine",
  paymentMethodId: "pix",
  debtIds: [],
  amount: 42.25,
  payment: { method: "Pix", dueDate: "2026-10-05", customerNote: "" },
  providerExternalId: "receivable-a",
  pixId: "pix-a",
  pixPayload: "pix-payload",
  createdAt: "2026-10-01T12:00:00Z",
};

it("opens the independent payment flow directly from a persisted charge", async () => {
  const id = "11111111-1111-4111-8111-111111111111";
  mocks.charge.mockResolvedValue({
    ...charge,
    id,
    items: [],
    processing: { jobId: "job-a", status: "Pending" },
  });
  render(<ChargeSuccessClient />);
  expect(await screen.findByRole("link", { name: "Gerar pagamento" })).toHaveAttribute(
    "href",
    `/payments/new?chargeIds=${id}`,
  );
  expect(screen.getByText("Número da cobrança")).toBeVisible();
  expect(screen.getByText("10-123")).toBeVisible();
  expect(screen.queryByText(id)).not.toBeInTheDocument();
});

it("shows the external provider identifier with a generic label", async () => {
  mocks.charge.mockResolvedValue(charge);
  render(<ChargeSuccessClient />);

  expect(await screen.findByText("ID externo")).toBeVisible();
  expect(screen.getByText("receivable-a")).toBeVisible();
  expect(screen.queryByText("ID Nibo")).not.toBeInTheDocument();
});

it("shows the existing payment request and hides generation when unavailable", async () => {
  mocks.payment = {
    ...mocks.payment,
    eligible: false,
    blockReason: "ActiveRequestExists",
    activity: {
      latestRequest: {
        id: "existing-request",
        status: "Active",
        createdAt: "2026-10-09T12:00:00Z",
        expiresAt: "2099-10-30T12:00:00Z",
        expiredAt: null,
        canceledAt: null,
        completedAt: null,
        completionReason: null,
      },
    },
  };
  mocks.charge.mockResolvedValue({ ...charge, id: "11111111-1111-4111-8111-111111111111" });
  render(<ChargeSuccessClient />);
  expect(await screen.findByText("Aguardando pagamento")).toBeVisible();
  expect(screen.getByText("existing-request")).toBeVisible();
  expect(screen.queryByRole("link", { name: "Gerar pagamento" })).not.toBeInTheDocument();
});

it("shows received payment details on the charge screen", async () => {
  mocks.payment = {
    ...mocks.payment,
    amount: 42.25,
    settled: true,
    receivedAmount: 42.25,
    receivedAt: "2026-10-15T15:00:00Z",
    settledAt: "2026-10-15T15:30:00Z",
    receipts: [
      {
        id: "receipt-a",
        endToEndId: "E2E-A",
        amount: 42.25,
        receivedAt: "2026-10-15T15:00:00Z",
        paymentMethodName: "PIX",
        provider: "Cielo",
      },
    ],
  };
  mocks.charge.mockResolvedValue({ ...charge, id: "11111111-1111-4111-8111-111111111111" });
  render(<ChargeSuccessClient />);
  expect(await screen.findByText("Pagamento")).toBeVisible();
  expect(screen.getByText("Referência bancária")).toBeVisible();
  expect(screen.getByText("E2E-A")).toBeVisible();
  expect(screen.getByText("Baixa confirmada em")).toBeVisible();
});

it("shows one scheduled operation and refreshes the charge manually", async () => {
  mocks.charge.mockResolvedValue({
    ...charge,
    items: [{ id: "item-a", ordinal: 1, amount: 42.25, categoryId: "fine" }],
    processing: { jobId: "job-a", status: "Pending" },
  });
  render(<ChargeSuccessClient />);
  expect(await screen.findByText("Integração agendada.")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Atualizar integração" }));
  await waitFor(() => expect(mocks.charge).toHaveBeenCalledTimes(2));
});

it("shows preserved confirmations when the handler failed without an aggregate result", async () => {
  mocks.charge.mockResolvedValue({
    ...charge,
    items: [
      { id: "item-a", ordinal: 1, amount: 20, categoryId: "fine", providerExternalId: "confirmed" },
      { id: "item-b", ordinal: 2, amount: 22.25, categoryId: "fine" },
    ],
    processing: { jobId: "job-a", status: "Failed", result: null },
  });
  render(<ChargeSuccessClient />);
  expect(
    await screen.findByText(/Integração interrompida com 1 de 2 itens confirmados/),
  ).toBeInTheDocument();
});

it("shows partial confirmation and the rejected item without losing confirmed links", async () => {
  mocks.charge.mockResolvedValue({
    ...charge,
    items: [
      { id: "item-a", ordinal: 1, amount: 20, categoryId: "fine", providerExternalId: "confirmed" },
      { id: "item-b", ordinal: 2, amount: 22.25, categoryId: "fine" },
    ],
    processing: {
      jobId: "job-a",
      status: "Failed",
      result: {
        outcome: "Partial",
        totalItems: 2,
        confirmedItems: 1,
        indeterminateItems: 0,
        items: [
          { chargeItemId: "item-a", outcome: "Completed" },
          { chargeItemId: "item-b", outcome: "Rejected", errorCode: "Unauthorized" },
        ],
      },
    },
  });
  render(<ChargeSuccessClient />);
  expect(
    await screen.findByText(/Integração parcialmente concluída: 1 de 2 itens confirmados/),
  ).toBeInTheDocument();
  expect(screen.queryByText("Integração não concluída")).not.toBeInTheDocument();
  expect(screen.getByLabelText("Lançamento criado no sistema financeiro")).toBeVisible();
  expect(screen.queryByText("confirmed")).not.toBeInTheDocument();
});
beforeEach(() => {
  vi.clearAllMocks();
  mocks.payment = {
    eligible: true,
    blockReason: null,
  } as import("@/features/payments/types/payment.types").PaymentCharge;
  window.sessionStorage.clear();
  mocks.charge.mockResolvedValue({ ...charge, id: "11111111-1111-4111-8111-111111111111" });
});

it.each(["selected", "standalone"])(
  "shows persisted %s item postings without offering unavailable PIX",
  async (source) => {
    mocks.charge.mockResolvedValue({
      ...charge,
      source,
      providerExternalId: "",
      pixId: "",
      pixPayload: "",
      items: [
        {
          id: "2d77726f-7e8e-4eb8-a594-ef8e01379442",
          debtId: "debt-a",
          categoryId: "fine",
          amount: 42.25,
          ordinal: 1,
          provider: "OtherProvider",
          providerExternalId: "external-item-a",
          debt: {
            contract: "CT 10432",
            plate: "ABC1D23",
            product: "TRUST",
            fleet: "Matriz",
            occurrenceDate: "2026-09-08",
            dueDate: "2026-09-15",
            description: "Multa por excesso de velocidade",
            origin: "Yalla Digital",
          },
        },
      ],
      financialIntegration: { outcome: "Completed" },
    });
    render(<ChargeSuccessClient />);
    const list = await screen.findByRole("list", { name: "Lançamentos por item" });
    expect(within(list).getByText("CT 10432")).toBeVisible();
    expect(within(list).getByText("ABC1D23")).toBeVisible();
    expect(within(list).getByText("Multa por excesso de velocidade")).toBeVisible();
    expect(within(list).getByText("08/09/2026")).toBeVisible();
    expect(within(list).getByLabelText("Lançamento criado no sistema financeiro")).toBeVisible();
    expect(screen.queryByText("external-item-a")).not.toBeInTheDocument();
    expect(screen.queryByText("Referência Yalla")).not.toBeInTheDocument();
    expect(screen.queryByText("2d77726f-7e8e-4eb8-a594-ef8e01379442")).not.toBeInTheDocument();
    expect(screen.queryByText("OtherProvider")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Copiar código PIX" })).not.toBeInTheDocument();
  },
);

it("shows confirmed and unconfirmed persisted items after a partial provider result", async () => {
  mocks.charge.mockResolvedValue({
    ...charge,
    source: "selected",
    pixPayload: "",
    items: [
      {
        id: "item-a",
        debtId: "debt-a",
        categoryId: "fine",
        amount: 20,
        ordinal: 1,
        provider: "TestFinancialProvider",
        providerExternalId: "confirmed-posting",
      },
      {
        id: "item-b",
        debtId: "debt-b",
        categoryId: "fine",
        amount: 22.25,
        ordinal: 2,
        provider: "TestFinancialProvider",
        providerExternalId: null,
      },
    ],
    financialIntegration: {
      outcome: "Indeterminate",
      errorCode: "IntegrationPendingOrUnconfirmed",
    },
  });
  render(<ChargeSuccessClient />);
  expect(await screen.findByLabelText("Lançamento criado no sistema financeiro")).toBeVisible();
  expect(screen.queryByText("TestFinancialProvider")).not.toBeInTheDocument();
  expect(screen.queryByText("confirmed-posting")).not.toBeInTheDocument();
  expect(screen.queryByText("Sem confirmação externa")).not.toBeInTheDocument();
  expect(screen.queryByText("Aguardando integração")).not.toBeInTheDocument();
});
afterEach(cleanup);

it("recovers success without loading debts when the debt service is unavailable", async () => {
  mocks.debts.mockRejectedValue(new Error("Debt service unavailable"));
  render(
    <ChargesLayout>
      <ChargeSuccessPage />
    </ChargesLayout>,
  );
  expect(await screen.findByRole("heading", { name: "Cobrança gerada com sucesso" })).toBeVisible();
  expect(mocks.debts).not.toHaveBeenCalled();
});

it("loads success from the API with empty browser storage and offers payment generation", async () => {
  render(<ChargeSuccessClient />);
  expect(await screen.findByRole("heading", { name: "Cobrança gerada com sucesso" })).toBeVisible();
  expect(screen.queryByText("Forma de pagamento")).not.toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Gerar pagamento" })).toHaveAttribute(
    "href",
    "/payments/new?chargeIds=11111111-1111-4111-8111-111111111111",
  );
  expect(mocks.charge).toHaveBeenCalledWith(
    "charge-a",
    expect.objectContaining({
      contextId: "session-test",
      signal: expect.any(AbortSignal),
    }),
  );
  expect(window.sessionStorage.getItem("yalla-finance.charges.session-test")).toBeNull();
});

it("does not display a charge the backend did not find in the current tenant", async () => {
  mocks.charge.mockResolvedValue(null);
  render(<ChargeSuccessClient />);
  expect(
    await screen.findByText("Não foi possível recuperar os dados da cobrança gerada."),
  ).toBeVisible();
  expect(
    screen.queryByRole("heading", { name: "Cobrança gerada com sucesso" }),
  ).not.toBeInTheDocument();
});

it("never presents a rejected integration as completed success", async () => {
  mocks.charge.mockResolvedValue({
    ...charge,
    financialIntegration: { outcome: "Rejected", errorCode: "ReceivableConflict" },
  });
  render(<ChargeSuccessClient />);
  expect(
    await screen.findByText(/Cobrança criada no Yalla \(10-123\), com integração rejeitada/),
  ).toBeVisible();
  expect(screen.queryByRole("button", { name: "Copiar código PIX" })).not.toBeInTheDocument();
});

it("recovers a failed charge query after an explicit retry without creating another charge", async () => {
  mocks.charge.mockRejectedValueOnce(new ApiError("API_UNAVAILABLE", "API indisponível", 503));
  render(<ChargeSuccessClient />);
  expect(await screen.findByText("Não foi possível carregar a cobrança.")).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
  expect(await screen.findByRole("heading", { name: "Cobrança gerada com sucesso" })).toBeVisible();
  expect(mocks.charge).toHaveBeenCalledTimes(2);
});
