import { act, cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { PaymentChargesPageClient } from "./PaymentChargesPageClient";

const first = "11111111-1111-4111-8111-111111111111";
const second = "22222222-2222-4222-8222-222222222222";

it("displays the exact charge reference in the table and details while preserving UUID navigation", () => {
  const result = mockUsePaymentCharges({ page: 1 });
  result.data.items[0].reference = "10-9223372036854775807";
  mockUsePaymentCharges.mockReturnValue(result);
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  expect(screen.getByText("10-9223372036854775807")).toBeVisible();
  fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[0]);
  const dialog = screen.getByRole("dialog", { name: "Detalhes da cobrança" });
  expect(within(dialog).getByText("10-9223372036854775807")).toBeVisible();
  expect(within(dialog).queryByText(first)).not.toBeInTheDocument();
});

it("applies exact charge reference with existing filters and rejects invalid input locally", () => {
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  fireEvent.change(screen.getByLabelText("Número da cobrança"), { target: { value: "10-9223372036854775807" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  expect(mockUsePaymentCharges).toHaveBeenLastCalledWith(expect.objectContaining({ reference: "10-9223372036854775807", page: 1 }));
  const calls = mockUsePaymentCharges.mock.calls.length;
  fireEvent.change(screen.getByLabelText("Número da cobrança"), { target: { value: "10-9223372036854775808" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  expect(screen.getByText("Informe a cobrança no formato empresa-número, como 10-123.")).toBeVisible();
  expect(mockUsePaymentCharges.mock.calls.slice(calls).every(([filters]) => filters.reference !== "10-9223372036854775808")).toBe(true);
});
const { mockUsePaymentCharges } = vi.hoisted(() => ({ mockUsePaymentCharges: vi.fn() }));
const { link, copy } = vi.hoisted(() => ({ link: vi.fn(), copy: vi.fn() }));
vi.mock("../api/payments.client", () => ({ paymentsApi: { link } }));
vi.mock("@/utils/clipboard", () => ({ copyTextToClipboard: copy }));
vi.mock("../hooks/usePaymentCharges", () => ({ usePaymentCharges: mockUsePaymentCharges }));
mockUsePaymentCharges.mockImplementation((filters: { page: number; paymentStatus?: string }) => ({
  refetch: vi.fn().mockResolvedValue({}),
  isFetching: false, isError: false, data: { page: filters.page, pageSize: 1,
    totalItems: 2, totalAmount: 150, awaitingPaymentCount: 7, expiredPaymentCount: 4, availablePaymentCount: 9,
    items: [{ id: filters.page === 1 ? first : second, customerName: "Locatário", contract: "Contrato 1", amount: filters.page === 1 ? 100 : 50,
      createdAt: "2026-10-09T12:00:00Z", dueDate: "2026-10-30", eligible: true, blockReason: null,
      activity: { latestRequest: { id: "request-id", status: filters.page === 1 ? "Active" : "Expired",
        createdAt: "2026-10-09T12:00:00Z", expiresAt: filters.page === 1 ? "2099-10-09T12:00:00Z" : "2000-10-09T12:00:00Z",
        expiredAt: null, canceledAt: null, completedAt: null, completionReason: null } } }],
  },
}));
afterEach(() => {
  cleanup();
  link.mockReset();
  copy.mockReset();
  mockUsePaymentCharges.mockClear();
  mockUsePaymentCharges.mockImplementation((filters: { page: number; paymentStatus?: string }) => ({
    refetch: vi.fn().mockResolvedValue({}),
    isFetching: false, isError: false, data: { page: filters.page, pageSize: 1,
      totalItems: 2, totalAmount: 150, awaitingPaymentCount: 7, expiredPaymentCount: 4, availablePaymentCount: 9,
      items: [{ id: filters.page === 1 ? first : second, customerName: "LocatÃ¡rio", contract: "Contrato 1", amount: filters.page === 1 ? 100 : 50,
        createdAt: "2026-10-09T12:00:00Z", dueDate: "2026-10-30", eligible: true, blockReason: null,
        activity: { latestRequest: { id: "request-id", status: filters.page === 1 ? "Active" : "Expired",
          createdAt: "2026-10-09T12:00:00Z", expiresAt: filters.page === 1 ? "2099-10-09T12:00:00Z" : "2000-10-09T12:00:00Z",
          expiredAt: null, canceledAt: null, completedAt: null, completionReason: null } } }],
    },
  }));
});

function openActiveRequest() {
  const result = mockUsePaymentCharges({ page: 1 });
  result.data.items[0].activePaymentRequest = {
    id: "request-id", status: "Active", expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  };
  mockUsePaymentCharges.mockReturnValue(result);
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[0]);
}

it("fetches the current link only on copy and fetches again for each click", async () => {
  link.mockResolvedValueOnce({ id: "request-id", url: "https://first.example/pay/token" })
    .mockResolvedValueOnce({ id: "request-id", url: "https://updated.example/pay/token" });
  copy.mockResolvedValue({ ok: true });
  openActiveRequest();
  expect(link).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Copiar link de pagamento" }));
  await waitFor(() => expect(copy).toHaveBeenCalledWith("https://first.example/pay/token"));
  fireEvent.click(screen.getByRole("button", { name: "Copiar link de pagamento" }));
  await waitFor(() => expect(copy).toHaveBeenCalledWith("https://updated.example/pay/token"));
  expect(link).toHaveBeenCalledTimes(2);
});

it("releases loading after a lookup failure and retries without reporting missing configuration", async () => {
  let rejectLookup!: (reason: Error) => void;
  link.mockImplementationOnce(() => new Promise((_, reject) => { rejectLookup = reject; }))
    .mockResolvedValueOnce({ id: "request-id", url: "https://checkout.example/pay/token" });
  copy.mockResolvedValue({ ok: true });
  openActiveRequest();
  fireEvent.click(screen.getByRole("button", { name: "Copiar link de pagamento" }));
  expect(screen.getByRole("dialog", { name: "Processando solicitação..." })).toBeInTheDocument();
  await act(async () => rejectLookup(new Error("Unavailable")));
  expect(screen.queryByRole("dialog", { name: "Processando solicitação..." })).not.toBeInTheDocument();
  expect(screen.getByText("Não foi possível copiar o link. Tente novamente.")).toBeVisible();
  expect(screen.queryByText(/Configure o endereço do checkout/)).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Copiar link de pagamento" }));
  await waitFor(() => expect(copy).toHaveBeenCalledWith("https://checkout.example/pay/token"));
});

it("reports missing configuration only when lookup succeeds without a URL", async () => {
  link.mockResolvedValue({ id: "request-id", url: null });
  openActiveRequest();
  fireEvent.click(screen.getByRole("button", { name: "Copiar link de pagamento" }));
  expect(await screen.findByText(/Configure o endereço do checkout/)).toBeVisible();
  expect(copy).not.toHaveBeenCalled();
  expect(screen.queryByRole("dialog", { name: "Processando solicitação..." })).not.toBeInTheDocument();
});

it("preserves charge selection across pages and navigates to a standalone generation route", async () => {
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  expect(screen.queryByRole("link", { name: "Gerar pagamento" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("checkbox", { name: `Selecionar cobrança ${first}` }));
  fireEvent.click(screen.getByRole("button", { name: "Go to page 2" }));
  fireEvent.click(screen.getByRole("checkbox", { name: `Selecionar cobrança ${second}` }));
  expect(screen.getByText("2 cobranças selecionadas")).toBeVisible();
  expect(screen.getByRole("link", { name: "Gerar pagamento" })).toHaveAttribute("href", `/payments/new?chargeIds=${first},${second}`);
  fireEvent.click(screen.getByText("Valor", { selector: "span" }));
  expect(screen.getByRole("columnheader", { name: "Valor" })).toHaveAttribute("aria-sort", "ascending");
  expect(screen.getByRole("checkbox", { name: `Selecionar cobrança ${first}` })).toBeChecked();
  expect(screen.getByText("2 cobranças selecionadas")).toBeVisible();
  fireEvent.click(screen.getByText("Valor", { selector: "span" }));
  expect(screen.getByRole("columnheader", { name: "Valor" })).toHaveAttribute("aria-sort", "descending");
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  expect(screen.queryByText("2 cobranças selecionadas")).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Categoria")).not.toBeInTheDocument();
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Atualizando cobranças..." })).not.toBeInTheDocument());
});

it.each([false, true])("refreshes unchanged filters and releases blocking loading (failure: %s)", async (failure) => {
  const result = mockUsePaymentCharges({ page: 1 });
  let finish!: () => void;
  result.refetch = vi.fn(() => new Promise<void>((resolve) => { finish = resolve; }));
  mockUsePaymentCharges.mockReturnValue(result);
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);

  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));

  expect(result.refetch).toHaveBeenCalledTimes(1);
  expect(screen.getByRole("dialog", { name: "Atualizando cobranças..." })).toBeInTheDocument();
  await act(async () => {
    result.isError = failure;
    result.data.items[0].customerName = "Cliente atualizado";
    finish();
  });
  expect(screen.queryByRole("dialog", { name: "Atualizando cobranças..." })).not.toBeInTheDocument();
  if (failure) {
    expect(screen.getByText("Não foi possível carregar as cobranças.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Tentar novamente" })).toBeVisible();
  } else {
    expect(screen.getByText("Cliente atualizado")).toBeVisible();
  }
});

it("opens the charge detail modal without exposing a link status", async () => {
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[0]);
  const dialog = screen.getByRole("dialog", { name: "Detalhes da cobrança" });
  expect(dialog).toBeVisible();
  expect(within(dialog).getAllByText("Status do pagamento")[0]).toBeVisible();
  expect(within(dialog).getAllByText("Disponível para solicitar pagamento")[0]).toBeVisible();
  fireEvent.click(within(dialog).getByRole("button", { name: "Fechar detalhes da cobrança" }));
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  expect(within(dialog).queryByText("Status do link")).not.toBeInTheDocument();
});

it("shows receipt and settlement details instead of an active request for a settled charge", () => {
  mockUsePaymentCharges.mockImplementation((filters: { page: number }) => ({
    isFetching: false, isError: false, data: { page: filters.page, pageSize: 5, totalItems: 1, totalAmount: 100,
      awaitingPaymentCount: 0, expiredPaymentCount: 0, availablePaymentCount: 0,
      items: [{ id: first, customerName: "Locatário", contract: "Contrato 1", amount: 100,
        createdAt: "2026-10-09T12:00:00Z", dueDate: "2026-10-30", eligible: false,
        blockReason: "ChargeSettled", settled: true, receivedAmount: 100, receivedAt: "2026-10-15T15:00:00Z",
        settledAt: "2026-10-15T15:30:00Z",
        activity: { latestRequest: { id: "request-id", status: "Completed", createdAt: "2026-10-09T12:00:00Z",
          expiresAt: "2026-10-10T12:00:00Z", expiredAt: null, canceledAt: null, completedAt: "2026-10-15T15:30:00Z", completionReason: null } } }],
    },
  }));
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[0]);
  const dialog = screen.getByRole("dialog", { name: "Detalhes da cobrança" });
  expect(within(dialog).getByText("Total recebido")).toBeVisible();
  expect(within(dialog).getAllByText(/100,00/).length).toBeGreaterThan(0);
  expect(within(dialog).getByText("Baixa confirmada em")).toBeVisible();
  expect(within(dialog).getAllByText(/15\/10\/2026/)).toHaveLength(2);
  expect(within(dialog).queryByText("Última solicitação")).not.toBeInTheDocument();
  expect(within(dialog).queryByText("Disponibilidade")).not.toBeInTheDocument();
});

it("shows status totals in clickable indicators and applies the selected status filter", () => {
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  const awaitingIndicator = screen.getByRole("button", { name: "Aguardando pagamento: 7" });
  const expiredIndicator = screen.getByRole("button", { name: "Pagamento expirado: 4" });
  const availableIndicator = screen.getByRole("button", { name: "Disponível: 9" });
  expect(awaitingIndicator).toBeVisible();
  expect(expiredIndicator).toBeVisible();
  expect(availableIndicator).toBeVisible();

  fireEvent.click(availableIndicator);
  expect(mockUsePaymentCharges.mock.calls.some(([filters]) => filters.paymentStatus === "available" && filters.page === 1)).toBe(true);
  expect(screen.getByRole("button", { name: "Disponível: 9" })).toHaveAttribute("aria-pressed", "true");

  fireEvent.click(expiredIndicator);

  expect(mockUsePaymentCharges.mock.calls.some(([filters]) => filters.paymentStatus === "expired" && filters.page === 1)).toBe(true);
  expect(screen.getByRole("button", { name: "Pagamento expirado: 4" })).toHaveAttribute("aria-pressed", "true");
  expect(screen.queryByText("Cobranças selecionadas")).not.toBeInTheDocument();
});

it("offers the same payment situations in the selectable date filter", () => {
  renderWithSession(<ToastProvider><PaymentChargesPageClient /></ToastProvider>);
  const statusFilter = screen.getByLabelText("Situação");

  expect(within(statusFilter).getByRole("option", { name: "Todas" })).toBeInTheDocument();
  expect(within(statusFilter).getByRole("option", { name: "Aguardando pagamento" })).toBeInTheDocument();
  expect(within(statusFilter).getByRole("option", { name: "Pagamento expirado" })).toBeInTheDocument();
  expect(within(statusFilter).getByRole("option", { name: "Disponível" })).toBeInTheDocument();

  fireEvent.change(statusFilter, { target: { value: "available" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));

  expect(mockUsePaymentCharges.mock.calls.some(([filters]) => filters.paymentStatus === "available")).toBe(true);
  expect(screen.getByRole("button", { name: "Disponível: 9" })).toHaveAttribute("aria-pressed", "true");
});
