import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { PaymentRequestForm } from "./PaymentRequestForm";
import type { PaymentPreparation } from "../types/payment.types";

const mocks = vi.hoisted(() => ({ generate: vi.fn(), push: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("../api/payments.client", () => ({ paymentsApi: { generate: mocks.generate } }));
const preparation: PaymentPreparation = {
  charges: [{ id: "11111111-1111-4111-8111-111111111111", reference: "10-123", customerName: "Cliente direto", contract: "C1", amount: 42,
    eligible: true, blockReason: null, createdAt: "2026-10-09T12:00:00Z", dueDate: "2026-10-30" }],
  methods: [{ id: "22222222-2222-4222-8222-222222222222", name: "Pix", code: "Pix", instrumentType: "Pix" }], maximumLinkDays: 30,
};
beforeEach(() => {
  vi.useFakeTimers({ toFake: ["Date"] });
  vi.setSystemTime(new Date("2026-10-10T01:30:00Z"));
  sessionStorage.clear(); mocks.generate.mockReset(); mocks.push.mockReset();
});
afterEach(() => { cleanup(); vi.useRealTimers(); });

it("limits a batch by the earliest due date and rejects expired charges", () => {
  const charges = [
    { ...preparation.charges[0], dueDate: "2026-10-20" },
    { ...preparation.charges[0], id: "66666666-6666-4666-8666-666666666666", dueDate: "2026-10-12" },
  ];
  const view = renderWithSession(<PaymentRequestForm preparation={{ ...preparation, charges }} onRemove={vi.fn()} />);
  const date = screen.getByLabelText(/Validade do link/);
  expect(screen.getAllByText("Cobrança 10-123")).toHaveLength(2);
  expect(date).toHaveAttribute("max", "2026-10-12");
  fireEvent.change(date, { target: { value: "2026-10-13" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeDisabled();
  expect(screen.getByText(/não pode ultrapassar o vencimento/)).toBeVisible();
  view.unmount();
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, charges: [{ ...charges[0], dueDate: "2026-10-08" }] }} onRemove={vi.fn()} />);
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeDisabled();
  expect(mocks.generate).not.toHaveBeenCalled();
});

it("blocks generation when the last selected method is unchecked and enables it when selected again", () => {
  renderWithSession(<PaymentRequestForm preparation={preparation} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText(/Validade do link/), { target: { value: "2026-10-15" } });
  const generate = screen.getByRole("button", { name: "Gerar requisições de pagamento" });
  fireEvent.click(screen.getByRole("checkbox", { name: "Pix" }));
  expect(generate).toBeDisabled();
  expect(screen.getByText("Selecione pelo menos uma forma de pagamento para continuar.")).toBeVisible();
  fireEvent.click(generate);
  expect(mocks.generate).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("checkbox", { name: "Pix" }));
  expect(generate).toBeEnabled();
});

it("reuses the same command after an uncertain response and prevents simultaneous clicks", async () => {
  mocks.generate.mockRejectedValueOnce(new Error("timeout"));
  mocks.generate.mockResolvedValueOnce({ items: [{ chargeId: preparation.charges[0].id, request: { id: "33333333-3333-4333-8333-333333333333" } }] });
  renderWithSession(<PaymentRequestForm preparation={preparation} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText(/Validade do link/), { target: { value: "2026-10-15" } });
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  await screen.findByText("timeout");
  expect(screen.getByLabelText(/Validade do link/)).toBeEnabled();
  expect(screen.getByLabelText("Nota interna")).toBeEnabled();
  expect(screen.getByRole("checkbox", { name: "Pix" })).toBeEnabled();
  const first = mocks.generate.mock.calls[0][0];
  cleanup();
  vi.setSystemTime(new Date("2026-10-16T12:00:00Z"));
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, methods: [],
    charges: preparation.charges.map((charge) => ({ ...charge, eligible: false, blockReason: "IneligibleDebts" })) }} onRemove={vi.fn()} />);
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeEnabled();
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  await waitFor(() => expect(mocks.push).toHaveBeenCalledWith("/payments/result?requestIds=33333333-3333-4333-8333-333333333333"));
  expect(mocks.generate.mock.calls[1][0]).toEqual(first);
  expect(Object.keys(sessionStorage).filter((key) => key.startsWith("payment-intent:"))).toEqual([]);
});

it("resolves a partial response without locking failed charges into the old command", async () => {
  const second = "44444444-4444-4444-8444-444444444444";
  mocks.generate.mockResolvedValueOnce({ items: [
    { chargeId: preparation.charges[0].id, request: { id: "33333333-3333-4333-8333-333333333333" } },
    { chargeId: second, message: "Cobrança indisponível" },
  ] });
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, charges: [...preparation.charges,
    { ...preparation.charges[0], id: second }] }} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText(/Validade do link/), { target: { value: "2026-10-15" } });
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  await waitFor(() => expect(mocks.push).toHaveBeenCalled());
  expect(Object.keys(sessionStorage).filter((key) => key.startsWith("payment-intent:"))).toEqual([]);
  cleanup();
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, charges: [{ ...preparation.charges[0], id: second }] }} onRemove={vi.fn()} />);
  expect(screen.getByLabelText(/Validade do link/)).toBeEnabled();
});

it("generates a request even when checkout publication is unavailable", async () => {
  mocks.generate.mockResolvedValueOnce({ items: [{ chargeId: preparation.charges[0].id, request: { id: "33333333-3333-4333-8333-333333333333" } }] });
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation }} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText(/Validade do link/), { target: { value: "2026-10-15" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeEnabled();
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  await waitFor(() => expect(mocks.push).toHaveBeenCalledWith("/payments/result?requestIds=33333333-3333-4333-8333-333333333333"));
});

it("shows every method returned by the existing catalog without asking for a bank connection", () => {
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation,
    methods: [...preparation.methods, { id: "55555555-5555-4555-8555-555555555555", name: "Boleto existente", code: "Boleto" }] }} onRemove={vi.fn()} />);
  expect(screen.getByRole("checkbox", { name: "Pix" })).toBeEnabled();
  expect(screen.getByRole("checkbox", { name: "Boleto existente" })).toBeEnabled();
  expect(screen.getByText("Pix")).toBeVisible();
  expect(screen.getByText("Boleto existente")).toBeVisible();
});

it("allows at most one payment method per instrument type", () => {
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, methods: [
    { ...preparation.methods[0], name: "Pix Sicob" },
    { id: "55555555-5555-4555-8555-555555555555", name: "Pix Asaas", code: "PixAsaas", instrumentType: "Pix" },
    { id: "66666666-6666-4666-8666-666666666666", name: "Cartão Asaas", code: "CreditCard", instrumentType: "CreditCard" },
  ] }} onRemove={vi.fn()} />);
  expect(screen.getByRole("checkbox", { name: "Pix Sicob" })).toBeChecked();
  expect(screen.getByRole("checkbox", { name: "Pix Asaas" })).toBeDisabled();
  expect(screen.getByRole("checkbox", { name: "Cartão Asaas" })).toBeEnabled();
  fireEvent.click(screen.getByRole("checkbox", { name: "Pix Sicob" }));
  expect(screen.getByRole("checkbox", { name: "Pix Sicob" })).not.toBeChecked();
  expect(screen.getByRole("checkbox", { name: "Pix Asaas" })).toBeEnabled();
});

it("defaults to today in Sao Paulo and submits a future deadline at the end of that day", async () => {
  mocks.generate.mockResolvedValueOnce({ items: [{ chargeId: preparation.charges[0].id, request: { id: "33333333-3333-4333-8333-333333333333" } }] });
  renderWithSession(<PaymentRequestForm preparation={preparation} onRemove={vi.fn()} />);
  expect(screen.getByLabelText(/Validade do link/)).toHaveValue("2026-10-09");
  expect(screen.getByLabelText(/Validade do link/)).toBeRequired();
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  await waitFor(() => expect(mocks.generate).toHaveBeenCalled());
  expect(mocks.generate.mock.calls[0][0].expiresAt).toBe("2026-10-10T02:59:59.999Z");
});

it("blocks generation when expiration is cleared", () => {
  renderWithSession(<PaymentRequestForm preparation={preparation} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText(/Validade do link/), { target: { value: "" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeDisabled();
  expect(screen.getByText("Informe a validade do link.")).toBeVisible();
  expect(mocks.generate).not.toHaveBeenCalled();
});

it("blocks the screen during generation and releases the form after a failure", async () => {
  let rejectGeneration!: (reason: Error) => void;
  mocks.generate.mockImplementationOnce(() => new Promise((_, reject) => { rejectGeneration = reject; }));
  renderWithSession(<PaymentRequestForm preparation={preparation} onRemove={vi.fn()} />);
  fireEvent.change(screen.getByLabelText("Nota interna"), { target: { value: "Preservar nota" } });
  fireEvent.click(screen.getByRole("button", { name: "Gerar requisições de pagamento" }));
  expect(await screen.findByRole("status", { name: "Gerando solicitações..." })).toBeVisible();
  rejectGeneration(new Error("Falha simulada"));
  await screen.findByText("Falha simulada");
  await waitFor(() => expect(screen.queryByRole("status", { name: "Gerando solicitações..." })).not.toBeInTheDocument());
  expect(screen.getByLabelText("Nota interna")).toHaveValue("Preservar nota");
  expect(screen.getByLabelText("Nota interna")).toBeEnabled();
});

it("blocks a past deadline and a date beyond the tenant limit", () => {
  renderWithSession(<PaymentRequestForm preparation={{ ...preparation, charges: preparation.charges.map((charge) => ({ ...charge, dueDate: "2026-12-01" })) }} onRemove={vi.fn()} />);
  const date = screen.getByLabelText(/Validade do link/);
  expect(date).toHaveAttribute("min", "2026-10-09");
  expect(date).toHaveAttribute("max", "2026-11-07");
  fireEvent.change(date, { target: { value: "2026-10-08" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeDisabled();
  expect(screen.getByText("Informe uma validade futura.")).toBeVisible();
  fireEvent.change(date, { target: { value: "2026-11-08" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeDisabled();
  expect(screen.getByText("A validade deve respeitar o limite de 30 dias desta empresa.")).toBeVisible();
  fireEvent.change(date, { target: { value: "2026-11-07" } });
  expect(screen.getByRole("button", { name: "Gerar requisições de pagamento" })).toBeEnabled();
});
