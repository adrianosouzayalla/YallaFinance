import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithSession as render } from "@/test/renderWithSession";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { NewChargeClient } from "@/features/charges/components/NewChargeClient";
import { getChargeToday } from "@/features/charges/businessRules";

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  setChargeIntent: vi.fn(),
  toast: vi.fn(),
  findMovements: vi.fn(),
  paymentMethods: vi.fn(),
  debts: vi.fn(),
  suggestions: vi.fn(),
}));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: mocks.push }),
  useSearchParams: () => new URLSearchParams(),
}));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast: mocks.toast }) }));
const customer = {
  id: "customer-joao",
  customerId: "customer-joao",
  tenantId: "organization",
  name: "João da Silva",
  taxId: "12345678900",
  phone: "",
  email: "",
};
const movements = ["ABC1D23", "DEF4G56"].map((plate, index) => ({
  id: `movement-${index}`,
  customerId: customer.customerId,
  tenantId: customer.tenantId,
  occurrenceDate: "2026-09-24",
  plate,
  contract: "Contrato",
  product: "TRUST",
  fleet: "Frota",
  costCenter: "Centro",
}));
const repository = {
  findCustomers: async () => [customer],
  listCategories: async () => [{ id: "category", name: "Multa" }],
  listPaymentMethods: mocks.paymentMethods,
  findFleetMovements: mocks.findMovements,
};
vi.mock("@/features/charges/context/ChargeDraftProvider", () => ({
  useChargeDraft: () => ({ setChargeIntent: mocks.setChargeIntent }),
}));
vi.mock("@/features/debts/api/debts.client", () => ({
  debtsApi: { listDebts: (...args: unknown[]) => mocks.debts(...args) },
}));
vi.mock("@/features/customers/api/customers.client", () => ({
  customersApi: {
    findCustomers: () => repository.findCustomers(),
    suggestCustomers: (...args: unknown[]) => mocks.suggestions(...args),
    findFleetMovements: (...args: unknown[]) => mocks.findMovements(...args),
  },
}));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: () => repository.listCategories() },
}));
vi.mock("@/features/catalogs/api/payment-methods.client", () => ({
  paymentMethodsApi: { listPaymentMethods: (...args: unknown[]) => mocks.paymentMethods(...args) },
}));

beforeEach(() => {
  vi.clearAllMocks();
  mocks.findMovements.mockResolvedValue(movements);
  mocks.suggestions.mockResolvedValue([customer]);
  mocks.paymentMethods.mockResolvedValue([{ id: "pix", name: "PIX" }]);
  mocks.debts.mockResolvedValue({
    items: [{ customerId: customer.customerId, categoryId: "category", occurrenceDate: "2026-09-24" }],
  });
});
afterEach(cleanup);

it("enables continuation for João after explicitly selecting DEF4G56 among two movements", async () => {
  const user = userEvent.setup();
  render(<NewChargeClient />);
  const detailInput = await screen.findByRole("textbox", { name: /Detalhe da descrição/ });
  expect(detailInput).toBeRequired();
  expect(detailInput).toHaveValue("Cobrança avulsa");
  fireEvent.change(detailInput, { target: { value: "" } });
  fireEvent.blur(detailInput);
  await waitFor(() => expect(detailInput).toHaveValue("Cobrança avulsa"));
  expect(screen.getByRole("button", { name: /Continuar/ })).toHaveAccessibleDescription(
    /Selecione o locatário/,
  );
  async function select(label: string, option: RegExp) {
    await user.click(screen.getByRole("combobox", { name: label }));
    await user.click(await screen.findByRole("option", { name: option }));
  }
  await user.type(screen.getByRole("combobox", { name: "Locatário" }), "Joã");
  await user.click(await screen.findByRole("option", { name: /João da Silva/ }));
  await select("Categoria", /Multa/);
  fireEvent.change(screen.getByLabelText("Valor"), { target: { value: "100,00" } });
  fireEvent.change(screen.getByLabelText("Data da ocorrência"), {
    target: { value: "2026-09-24" },
  });
  fireEvent.change(await screen.findByRole("textbox", { name: /Detalhe da descrição/ }), {
    target: { value: "Descrição da multa" },
  });
  expect(screen.queryByRole("combobox", { name: "Forma de pagamento" })).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Vencimento"), { target: { value: getChargeToday() } });
  expect(screen.getByRole("button", { name: /Continuar/ })).toBeDisabled();
  await waitFor(() =>
    expect(screen.getByRole("combobox", { name: "Placa do veículo" })).toBeVisible(),
  );
  expect(screen.getByRole("button", { name: /Continuar/ })).toHaveAccessibleDescription(
    /Selecione a placa do veículo/,
  );
  await select("Placa do veículo", /DEF4G56/);
  expect(screen.queryByText(/Aten.*j.*existe um d.*bito/)).not.toBeInTheDocument();
  await waitFor(() => expect(screen.getByRole("button", { name: /Continuar/ })).toBeEnabled());
  expect(screen.queryByText(/Para continuar:/)).not.toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: /Continuar/ }));
  expect(mocks.findMovements).toHaveBeenCalledWith(
    "customer-joao",
    "2026-09-24",
    expect.objectContaining({ signal: expect.any(AbortSignal), contextId: "session-test" }),
  );
  expect(mocks.setChargeIntent).toHaveBeenCalledWith(
    expect.objectContaining({
      customerId: "customer-joao",
      customerName: "João da Silva",
      standalone: expect.objectContaining({
        amount: 100,
        customerId: "customer-joao",
        selectedMovementId: "movement-1",
        dueDate: getChargeToday(),
      }),
    }),
  );
  expect(mocks.push).toHaveBeenCalledWith("/charges/review?source=standalone");
}, 15000);

it("keeps the due date without asking for or loading payment methods", async () => {
  mocks.paymentMethods.mockResolvedValue([
    { id: "pix-a", name: "PIX A" },
    { id: "pix-b", name: "PIX B" },
  ]);
  render(<NewChargeClient />);
  expect(await screen.findByLabelText("Vencimento")).toBeInTheDocument();
  expect(screen.queryByRole("combobox", { name: "Forma de pagamento" })).not.toBeInTheDocument();
  expect(mocks.paymentMethods).not.toHaveBeenCalled();
});

it("uses a single customer field and searches only after three trimmed characters", async () => {
  const user = userEvent.setup();
  render(<NewChargeClient />);
  const input = await screen.findByRole("combobox", { name: "Locatário" });
  expect(screen.getAllByLabelText("Locatário")).toHaveLength(1);
  expect(mocks.suggestions).not.toHaveBeenCalled();
  await user.type(input, "  Jo");
  await new Promise((resolve) => setTimeout(resolve, 350));
  expect(mocks.suggestions).not.toHaveBeenCalled();
  await user.type(input, "ã");
  expect(mocks.suggestions).not.toHaveBeenCalled();
  await user.click(await screen.findByRole("option", { name: /João da Silva/ }));
  expect(mocks.suggestions).toHaveBeenCalledWith("Joã", expect.objectContaining({
    signal: expect.any(AbortSignal), contextId: "session-test",
  }));
  expect(input).toHaveValue("João da Silva (***.456.789-**)");
  await user.clear(input);
  expect(screen.getByRole("button", { name: /Continuar/ })).toBeDisabled();
}, 15000);
