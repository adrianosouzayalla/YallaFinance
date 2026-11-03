import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { PaymentGenerationResultClient } from "./PaymentGenerationResultClient";

const requestId = "11111111-1111-4111-8111-111111111111";
const mocks = vi.hoisted(() => ({ link: vi.fn() }));
vi.mock("../api/payments.client", () => ({ paymentsApi: { link: mocks.link } }));
vi.mock("next/navigation", () => ({ useSearchParams: () => new URLSearchParams(`requestIds=${requestId}`) }));
vi.mock("../hooks/usePaymentGenerationResults", () => ({ usePaymentGenerationResults: () => ({
  data: [{ id: requestId, chargeId: "22222222-2222-4222-8222-222222222222", status: "Active", createdAt: "2026-10-09T12:00:00Z", expiresAt: "2099-12-31T12:00:00Z",
    charge: { reference: "10-9223372036854775807", customerName: "Cliente final", contract: "C-01", amount: 50 } }], isError: false,
}) }));
beforeEach(() => { mocks.link.mockReset(); sessionStorage.clear(); });
afterEach(cleanup);

function renderResult() {
  return renderWithSession(<ToastProvider><PaymentGenerationResultClient /></ToastProvider>);
}

it("shows generated requests without filters or a charge selection mechanism", () => {
  renderResult();
  expect(screen.getByRole("table", { name: "Solicitações geradas" })).toBeVisible();
  expect(screen.getByText("Cliente final")).toBeVisible();
  expect(screen.getByText("10-9223372036854775807")).toBeVisible();
  expect(screen.queryByText("22222222-2222-4222-8222-222222222222")).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Filtrar" })).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Data inicial")).not.toBeInTheDocument();
  expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: /^Copiar link/ })).toBeVisible();
  expect(screen.queryByRole("button", { name: "Copiar token" })).not.toBeInTheDocument();
});

it("copies the complete checkout URL instead of its token", async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText } });
  mocks.link.mockResolvedValueOnce({ id: requestId, token: "original-safe-token", url: "https://checkout.example/pay/original-safe-token" });
  renderResult();
  fireEvent.click(screen.getByRole("button", { name: /^Copiar link/ }));
  await waitFor(() => expect(writeText).toHaveBeenCalledWith("https://checkout.example/pay/original-safe-token"));
  expect(writeText).not.toHaveBeenCalledWith("original-safe-token");
});

it("does not silently copy a token when the checkout URL is missing", async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText } });
  mocks.link.mockResolvedValueOnce({ id: requestId, token: "original-safe-token", url: null });
  renderResult();
  fireEvent.click(screen.getByRole("button", { name: /^Copiar link/ }));
  await screen.findByText(/endereço do checkout/);
  expect(writeText).not.toHaveBeenCalled();
});

it("keeps the table mounted while copying again", async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText } });
  mocks.link.mockResolvedValueOnce({ id: requestId, token: "original-safe-token", url: "https://checkout.example/pay/original-safe-token" });
  mocks.link.mockImplementationOnce(() => new Promise(() => {}));
  renderResult();
  const table = screen.getByRole("table", { name: "Solicitações geradas" });
  fireEvent.click(screen.getByRole("button", { name: /^Copiar link/ }));
  await screen.findByText("Link copiado.");
  fireEvent.click(screen.getByRole("button", { name: /^Copiar link/ }));
  expect(table).toBeInTheDocument();
  expect(table.closest('[aria-hidden="true"]')).not.toBeNull();
  expect(screen.getByRole("status", { name: "Processando solicitação..." })).toBeVisible();
});
