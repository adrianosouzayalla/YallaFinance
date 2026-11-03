import { act, cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { PaymentGenerationResultClient } from "./PaymentGenerationResultClient";
import { PaymentCart } from "./PaymentCart";
import { PaymentChargeTable } from "./PaymentChargeTable";
import { usePaymentCharges } from "../hooks/usePaymentCharges";
import { usePaymentPreparation } from "../hooks/usePaymentPreparation";
import { paymentsApi } from "../api/payments.client";
import type { PaymentRequest } from "../types/payment.types";

const state = vi.hoisted(() => ({ canceled: false, fail: false, financialRisk: false }));
const requestId = "11111111-1111-4111-8111-111111111111";
const chargeId = "22222222-2222-4222-8222-222222222222";
const filters = { startDate: "", endDate: "", page: 1, pageSize: 5 };
vi.mock("next/navigation", () => ({ useSearchParams: () => new URLSearchParams(`requestIds=${requestId}`) }));

function request() {
  return { id: requestId, chargeId, status: state.canceled ? "Canceled" : "Active", createdAt: "2026-10-09T12:00:00Z",
    expiresAt: "2030-10-30T12:00:00Z", replayed: false };
}
function charge() {
  return { id: chargeId, customerName: "Cliente", contract: "C1", amount: 42, createdAt: "2026-10-09T12:00:00Z",
    dueDate: "2026-10-30", eligible: state.canceled && !state.financialRisk,
    blockReason: state.canceled ? state.financialRisk ? "OpenFinancialRisk" : null : "ActiveRequestExists" };
}
vi.mock("../api/payments.client", () => ({ paymentsApi: {
  getRequest: vi.fn(async () => request()),
  prepare: async () => ({ charges: [charge()], methods: [], maximumLinkDays: 30 }),
  list: async () => ({ ...filters, items: [charge()], totalItems: 1, totalAmount: 42 }),
  cancel: async () => { if (state.fail) throw new Error("Cancelamento recusado"); state.canceled = true; return request(); },
} }));
beforeEach(() => { state.canceled = false; state.fail = false; state.financialRisk = false; vi.clearAllMocks(); sessionStorage.clear(); });
afterEach(cleanup);

function CurrentChargeViews() {
  const list = usePaymentCharges(filters); const preparation = usePaymentPreparation([chargeId]);
  return <>
    <section aria-label="Lista atual">{list.data && <PaymentChargeTable data={list.data} selected={[]} onSelect={() => {}}
      onClear={() => {}} onPage={() => {}} onSort={() => {}} loading={list.isFetching} />}</section>
    <section aria-label="Preparação atual">{preparation.data && <PaymentCart charges={preparation.data.charges}
      disabled={false} onRemove={() => {}} />}</section>
  </>;
}
async function openCancellation() {
  const rendered = renderWithSession(<ToastProvider><CurrentChargeViews /><PaymentGenerationResultClient /></ToastProvider>);
  await screen.findByRole("button", { name: "Cancelar link" });
  const preparation = screen.getByRole("region", { name: "Preparação atual" });
  await within(preparation).findByText("Aguardando pagamento");
  fireEvent.click(screen.getByRole("button", { name: "Cancelar link" }));
  return rendered;
}
function confirmCancellation() {
  fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cancelar link" }));
}

it("refreshes charge eligibility in both the list and preparation after cancellation", async () => {
  await openCancellation();
  confirmCancellation();
  await screen.findByText("Cancelado");
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  const list = screen.getByRole("region", { name: "Lista atual" });
  await within(list).findByText("Disponível");
  await waitFor(() => expect(within(screen.getByRole("region", { name: "Preparação atual" }))
    .queryByText("Aguardando pagamento")).not.toBeInTheDocument());
  expect(within(list).getByRole("checkbox", { name: `Selecionar cobrança ${chargeId}` })).toBeEnabled();
});

it("keeps the charge blocked when cancellation fails", async () => {
  state.fail = true;
  await openCancellation();
  confirmCancellation();
  await screen.findByText("Não foi possível cancelar o link.");
  fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Voltar" }));
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  expect(within(screen.getByRole("region", { name: "Preparação atual" }))
    .getByText("Aguardando pagamento")).toBeVisible();
  expect(within(screen.getByRole("region", { name: "Lista atual" }))
    .getByRole("checkbox", { name: `Selecionar cobrança ${chargeId}` })).toBeDisabled();
});

it("preserves a remaining financial block after canceling the link", async () => {
  state.financialRisk = true;
  await openCancellation();
  confirmCancellation();
  await screen.findByText("Cancelado");
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  const list = screen.getByRole("region", { name: "Lista atual" });
  await within(list).findByText("Pagamento em andamento");
  expect(within(list).getByRole("checkbox", { name: `Selecionar cobrança ${chargeId}` })).toBeDisabled();
  expect(within(screen.getByRole("region", { name: "Preparação atual" }))
    .getByText("Pagamento em andamento")).toBeVisible();
});

it("does not restore active status when an older request read finishes after cancellation", async () => {
  const { client } = await openCancellation();
  const staleRequest = request();
  let finishRead!: (value: PaymentRequest) => void;
  vi.mocked(paymentsApi.getRequest).mockImplementationOnce(() => new Promise((resolve) => { finishRead = resolve; }));
  const pendingRead = client.refetchQueries({ queryKey: ["payment-results", "tenant-test", "session-test"] });
  await waitFor(() => expect(paymentsApi.getRequest).toHaveBeenCalledTimes(2));
  confirmCancellation();
  await screen.findByText("Cancelado");
  await act(async () => { finishRead(staleRequest); await pendingRead; });
  expect(client.getQueryData<PaymentRequest[]>(["payment-results", "tenant-test", "session-test", [requestId]])
    ?.find((row) => row.id === requestId)?.status).toBe("Canceled");
  expect(screen.getByText("Cancelado")).toBeVisible();
  expect(screen.queryByText("Ativo")).not.toBeInTheDocument();
});
