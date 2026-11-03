import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { PublicPaymentCheckoutClient } from "./PublicPaymentCheckoutClient";
import type { PublicPaymentCheckout } from "../../types/payment.types";
import { ApiError } from "@/services/api/api-client";
import userEvent from "@testing-library/user-event";

const { mockRefresh, mockStartAttempt } = vi.hoisted(() => ({ mockRefresh: vi.fn(), mockStartAttempt: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: mockRefresh }) }));
vi.mock("../../api/public-payment.client", () => ({ publicPaymentApi: { startAttempt: mockStartAttempt } }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

const token = "a".repeat(64);
const pix = { code: "PIX", name: "Pix", instrumentType: "Pix" as const };
const boleto = { code: "BOLETO", name: "Boleto bancário" };
const checkout = {
  status: "Active",
  expiresAt: "2026-12-31T23:59:59Z",
  amount: 125.5,
  dueDate: "2026-11-15",
  contractNumber: "CTR-2048",
  methods: [pix, boleto],
  attempt: null,
};
const result = {
  status: "AwaitingPayment",
  amount: 125.5,
  expiresAt: "2026-12-01T13:00:00Z",
  brCode: "00020101021226890014br.gov.bcb.pix",
  location: null,
  canRetry: false,
  message: null,
  instrumentType: "Pix",
};
const renderCheckout = (value: PublicPaymentCheckout = checkout) =>
  render(<PublicPaymentCheckoutClient token={token} checkout={value} />);

it.each([
  ["4111111111111111", "4111 1111 1111 1111"],
  ["4111 1111 1111 1111 111", "4111 1111 1111 1111 111"],
])("formats pasted card number %s without changing digits", async (pasted, formatted) => {
  const user = userEvent.setup();
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  const input = screen.getByLabelText("Número do cartão");
  await user.click(input);
  await user.paste(pasted);
  expect(input).toHaveValue(formatted);
});

it("limits card typing to 19 digits and supports deleting and clearing the formatted number", async () => {
  const user = userEvent.setup();
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  const input = screen.getByLabelText("Número do cartão");
  await user.type(input, "411111111111111111112345");
  expect(input).toHaveValue("4111 1111 1111 1111 111");
  await user.keyboard("{Backspace}");
  expect(input).toHaveValue("4111 1111 1111 1111 11");
  await user.clear(input);
  expect(input).toHaveValue("");
});

it("blocks oversized native input while the keyboard is composing", () => {
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  const input = screen.getByLabelText("Número do cartão");
  expect(input).toHaveAttribute("maxlength", "23");
  fireEvent.change(input, { target: { value: "4444444444444444" } });
  fireEvent.compositionStart(input);
  fireEvent.input(input, { target: { value: "4".repeat(75) + "5".repeat(20) }, isComposing: true });
  expect(input).toHaveValue("4444 4444 4444 4444");
  fireEvent.compositionEnd(input);
});

it("routes an opaque method code to the Pix component by capability", async () => {
  mockStartAttempt.mockResolvedValue(result);
  renderCheckout({ ...checkout, methods: [{ code: "BANK-1", name: "Pagamento instantâneo", instrumentType: "Pix" }] });
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));
  expect(await screen.findByLabelText("Pix copia e cola")).toHaveValue(result.brCode);
});

it("loads the card form in the same checkout without starting a payment", () => {
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }],
    cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  expect(screen.getByLabelText("Número do cartão")).toBeVisible();
  expect(screen.getByRole("checkbox", { name: "O titular é o cliente da cobrança" })).toBeChecked();
  expect(screen.getByTestId("CheckBoxIcon")).toBeVisible();
  expect(screen.getByText("CTR-2048")).toBeVisible();
  expect(mockStartAttempt).not.toHaveBeenCalled();
  expect(screen.queryByLabelText("CPF/CNPJ do titular")).not.toBeInTheDocument();
  fireEvent.click(screen.getByLabelText("O titular é o cliente da cobrança"));
  expect(screen.getByLabelText("CPF/CNPJ do titular")).toBeVisible();
});

it("submits only card data when the customer profile is complete and clears sensitive fields", async () => {
  mockStartAttempt.mockResolvedValue({ ...result, status: "Succeeded", brCode: null, instrumentType: "CreditCard" });
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  fireEvent.change(screen.getByLabelText("Nome no cartão"), { target: { value: "Titular Teste" } });
  fireEvent.change(screen.getByLabelText("Número do cartão"), { target: { value: "4111 1111 1111 1111" } });
  fireEvent.change(screen.getByLabelText("Validade (MM/YYYY)"), { target: { value: "12/2035" } });
  fireEvent.change(screen.getByLabelText("CVV"), { target: { value: "123" } });
  fireEvent.click(screen.getByRole("button", { name: "Pagar com cartão" }));
  await waitFor(() => expect(mockStartAttempt).toHaveBeenCalledOnce());
  expect(mockStartAttempt).toHaveBeenCalledWith(token, "CC", expect.any(String), expect.objectContaining({
    number: "4111111111111111", expiryMonth: "12", expiryYear: "2035", sameHolderAsCustomer: true, holder: {},
  }));
  expect(screen.queryByLabelText("Número do cartão")).not.toBeInTheDocument();
});

it("hides third party fields without validating hidden data when the payer returns to same customer", async () => {
  mockStartAttempt.mockResolvedValue({ ...result, status: "AwaitingPayment", brCode: null, instrumentType: "CreditCard" });
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  fireEvent.click(screen.getByLabelText("O titular é o cliente da cobrança"));
  expect(screen.getByLabelText("CPF/CNPJ do titular")).toBeVisible();
  fireEvent.click(screen.getByLabelText("O titular é o cliente da cobrança"));
  fireEvent.change(screen.getByLabelText("Nome no cartão"), { target: { value: "Titular Teste" } });
  fireEvent.change(screen.getByLabelText("Número do cartão"), { target: { value: "4111111111111111" } });
  fireEvent.change(screen.getByLabelText("Validade (MM/YYYY)"), { target: { value: "12/2035" } });
  fireEvent.change(screen.getByLabelText("CVV"), { target: { value: "123" } });
  fireEvent.click(screen.getByRole("button", { name: "Pagar com cartão" }));
  await waitFor(() => expect(mockStartAttempt).toHaveBeenCalledOnce());
  expect(screen.queryByLabelText("CPF/CNPJ do titular")).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Continuar" })).not.toBeInTheDocument();
});

it("does not allow another card submission after a lost response until status is refreshed", async () => {
  mockStartAttempt.mockRejectedValue(new Error("simulated connection loss"));
  renderCheckout({ ...checkout, methods: [{ code: "CC", name: "Cartão", instrumentType: "CreditCard" }], cardRequiredFields: [] });
  fireEvent.click(screen.getByRole("button", { name: "Continuar" }));
  for (const [label, value] of [["Nome no cartão", "Titular Teste"], ["Número do cartão", "4111111111111111"], ["Validade (MM/YYYY)", "12/2035"], ["CVV", "123"]])
    fireEvent.change(screen.getByLabelText(label), { target: { value } });
  fireEvent.click(screen.getByRole("button", { name: "Pagar com cartão" }));
  expect(await screen.findByRole("button", { name: "Atualizar situação" })).toBeVisible();
  expect(screen.queryByRole("button", { name: "Pagar com cartão" })).not.toBeInTheDocument();
  expect(mockStartAttempt).toHaveBeenCalledOnce();
});

it("shows a selected Pix card and enables the real checkout action when Pix is the only option", () => {
  renderCheckout({ ...checkout, methods: [pix] });
  expect(screen.getByRole("heading", { name: "Escolha como pagar" })).toBeVisible();
  expect(screen.getByText("CTR-2048")).toBeVisible();
  expect(screen.getByRole("radio", { name: /Pix/ })).toBeChecked();
  expect(screen.getByRole("button", { name: "Finalizar pagamento" })).toBeEnabled();
});

it("confirms a settled payment with its receipt date and prevents another payment", () => {
  renderCheckout({ ...checkout, status: "Completed", hasReceivedPayment: true, receivedAmount: 125.5,
    receivedAt: "2026-10-15T15:00:00Z", settled: true, settledAt: "2026-10-15T15:30:00Z",
    receipts: [{ amount: 125.5, receivedAt: "2026-10-15T15:00:00Z", paymentMethodName: "PIX" }] });
  expect(screen.getByRole("heading", { level: 1, name: "Pagamento confirmado" })).toBeVisible();
  expect(screen.getByText("Recebemos seu pagamento. Não é necessário pagar novamente.")).toBeVisible();
  expect(screen.getByRole("status")).toHaveTextContent("Pagamento realizado em 15/10/2026, 12:00.");
  expect(screen.getByLabelText(/Valor total R\$\s*125,50/)).toBeVisible();
  expect(screen.queryByRole("button", { name: "Finalizar pagamento" })).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Pix copia e cola")).not.toBeInTheDocument();
});

it("reports partial receipt amounts and pending settlement without offering another payment", () => {
  renderCheckout({ ...checkout, hasReceivedPayment: true, receivedAmount: 40, receivedAt: "2026-10-15T15:00:00Z",
    receipts: [{ amount: 40, receivedAt: "2026-10-15T15:00:00Z", paymentMethodName: "PIX" }] });
  expect(screen.getByRole("heading", { level: 1, name: "Pagamento recebido parcialmente" })).toBeVisible();
  expect(screen.getByText(/Recebemos R\$\s*40,00 de R\$\s*125,50; a confirmação da baixa está em andamento\./)).toBeVisible();
  expect(screen.getByRole("status")).toHaveTextContent(/Recebemos R\$\s*40,00 de R\$\s*125,50\. Ainda há saldo pendente\./);
  expect(screen.queryByRole("heading", { name: "Pagamento confirmado" })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Finalizar pagamento" })).not.toBeInTheDocument();
});

it("requires the payer to select one of multiple authorized methods", () => {
  renderCheckout();
  expect(screen.getByRole("radio", { name: /Pix/ })).toBeInTheDocument();
  expect(screen.getByRole("radio", { name: /Boleto bancário/ })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Finalizar pagamento" })).toBeDisabled();
  fireEvent.click(screen.getByRole("radio", { name: /Pix/ }));
  expect(screen.getByRole("button", { name: "Finalizar pagamento" })).toBeEnabled();
  fireEvent.click(screen.getByRole("radio", { name: /Boleto bancário/ }));
  expect(screen.getByRole("button", { name: "Finalizar pagamento" })).toBeDisabled();
  expect(screen.getByRole("status")).toHaveTextContent("A forma selecionada ainda não está disponível para pagamento.");
});

it("starts Pix using the selected method and displays the returned QR and copy code", async () => {
  mockStartAttempt.mockResolvedValue(result);
  renderCheckout({ ...checkout, methods: [pix] });
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));

  expect(await screen.findByRole("heading", { name: "Pix gerado" })).toBeVisible();
  expect(screen.getByLabelText("Pix copia e cola")).toHaveValue(result.brCode);
  expect(document.querySelector("svg")).toBeInTheDocument();
  expect(mockStartAttempt).toHaveBeenCalledOnce();
  expect(mockStartAttempt).toHaveBeenCalledWith(token, "PIX", expect.any(String));
});

it("shows the sanitized operational reason returned when Pix cannot start", async () => {
  mockStartAttempt.mockRejectedValue(new ApiError(
    "ServiceUnavailable",
    "A forma de pagamento está indisponível no momento.",
    503,
  ));
  renderCheckout({ ...checkout, methods: [pix] });
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("A forma de pagamento está indisponível no momento.");
});

it("shows a friendly message when the provider rejects Pix and returns to payment selection", async () => {
  mockStartAttempt.mockResolvedValue({
    ...result,
    status: "Failed",
    brCode: null,
    canRetry: true,
    message: "Não foi possível gerar o Pix. Você pode tentar novamente.",
    instrumentType: "Pix",
  });
  renderCheckout({ ...checkout, methods: [pix] });
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));

  expect(await screen.findByRole("heading", { name: "Escolha como pagar" })).toBeVisible();
  expect(await screen.findByRole("alert")).toHaveTextContent(
    "Não foi possível gerar o Pix. Você pode tentar novamente.",
  );
  expect(screen.getByRole("button", { name: "Gerar Pix" })).toBeEnabled();
});

it("does not show an old Pix failure when the payer opens the checkout", () => {
  renderCheckout({ ...checkout, methods: [pix], attempt: {
    ...result,
    status: "Failed",
    brCode: null,
    canRetry: true,
    message: "Não foi possível gerar o Pix. Você pode tentar novamente.",
    instrumentType: "Pix",
  } });

  expect(screen.getByRole("heading", { name: "Escolha como pagar" })).toBeVisible();
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
});

it("shows an operation error after retrying a previously failed Pix", async () => {
  mockStartAttempt.mockRejectedValue(new Error("simulated API error"));
  renderCheckout({ ...checkout, methods: [pix], attempt: {
    ...result,
    status: "Failed",
    brCode: null,
    canRetry: true,
    message: "Erro anterior",
    instrumentType: "Pix",
  } });

  fireEvent.click(screen.getByRole("button", { name: "Gerar Pix" }));

  expect(await screen.findByRole("alert")).toHaveTextContent(
    "Não foi possível confirmar seu pagamento agora. Tente novamente em instantes.",
  );
});

it("shows a persisted Pix and recovers it after the payer selects the same payment method again", async () => {
  mockStartAttempt.mockResolvedValue(result);
  renderCheckout({ ...checkout, methods: [pix], attempt: result });
  expect(screen.getByRole("heading", { name: "Pix gerado" })).toBeVisible();
  expect(screen.getByLabelText("Pix copia e cola")).toHaveValue(result.brCode);
  fireEvent.click(screen.getByRole("button", { name: "Mudar Forma de Pagamento" }));
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));
  await waitFor(() => expect(mockStartAttempt).toHaveBeenCalledOnce());
  expect(mockStartAttempt).toHaveBeenCalledWith(token, "PIX", expect.any(String));
});

it("returns to payment selection for an uncertain Pix and retries without showing an error", async () => {
  const uncertain = { ...result, status: "Unknown", brCode: null, message: "Não foi possível confirmar a emissão." };
  mockStartAttempt.mockResolvedValue(uncertain);
  renderCheckout({ ...checkout, methods: [pix], attempt: uncertain });
  expect(screen.getByRole("heading", { name: "Escolha como pagar" })).toBeVisible();
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  const generatePixButton = screen.getByRole("button", { name: "Gerar Pix" });
  expect(generatePixButton).toBeEnabled();
  expect(screen.queryByRole("button", { name: "Atualizar" })).not.toBeInTheDocument();
  fireEvent.click(generatePixButton);
  await waitFor(() => expect(mockStartAttempt).toHaveBeenCalledOnce());
  await waitFor(() => expect(screen.getByRole("button", { name: "Gerar Pix" })).toBeEnabled());
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
});

it("returns to payment selection after an uncertain Pix issuance without showing an error", async () => {
  const message = "NÃ£o foi possÃ­vel confirmar a emissÃ£o. Atualize a pÃ¡gina antes de tentar novamente.";
  mockStartAttempt.mockResolvedValue({ ...result, status: "Unknown", brCode: null, message });
  renderCheckout({ ...checkout, methods: [pix] });
  fireEvent.click(screen.getByRole("button", { name: "Finalizar pagamento" }));

  expect(await screen.findByRole("heading", { name: "Escolha como pagar" })).toBeVisible();
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  expect(screen.queryByText(message)).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Atualizar" })).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Gerar Pix" })).toBeEnabled();
});

it.each(["Expired", "Canceled"])("blocks a new attempt when the link state is %s", (status) => {
  renderCheckout({ ...checkout, status });
  expect(screen.getByRole("alert")).toBeVisible();
  expect(screen.queryByRole("button", { name: "Finalizar pagamento" })).not.toBeInTheDocument();
});

it("shows the received payment state instead of payment actions for a completed link", () => {
  renderCheckout({ ...checkout, status: "Completed" });
  expect(screen.getAllByRole("heading", { name: "Pagamento confirmado" }).length).toBeGreaterThan(0);
  expect(screen.queryByRole("button", { name: "Finalizar pagamento" })).not.toBeInTheDocument();
});

it("shows a retry for checkout loading errors", () => {
  render(<PublicPaymentCheckoutClient token={token} checkout={null} temporaryError />);
  expect(screen.getByRole("alert")).toHaveTextContent("Não foi possível carregar o checkout.");
  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
  expect(mockRefresh).toHaveBeenCalledOnce();
});

it("retains the last checkout and Pix when a background refresh temporarily returns no data", () => {
  const persisted = { ...checkout, methods: [pix], attempt: { ...result, paymentMethodCode: "PIX" } };
  const view = renderCheckout(persisted);
  view.rerender(<PublicPaymentCheckoutClient token={token} checkout={null} temporaryError />);
  expect(screen.getByLabelText("Pix copia e cola")).toHaveValue(result.brCode);
  expect(screen.queryByText("Não foi possível carregar o checkout. Tente novamente.")).not.toBeInTheDocument();
});

it("adopts an attempt received on refresh without submitting a new payment", () => {
  const view = renderCheckout();
  view.rerender(<PublicPaymentCheckoutClient token={token} checkout={{ ...checkout,
    attempt: { ...result, paymentMethodCode: "PIX" } }} />);
  expect(screen.getByRole("heading", { name: "Pix gerado" })).toBeVisible();
  expect(screen.getByLabelText("Pix copia e cola")).toHaveValue(result.brCode);
  expect(mockStartAttempt).not.toHaveBeenCalled();
});
