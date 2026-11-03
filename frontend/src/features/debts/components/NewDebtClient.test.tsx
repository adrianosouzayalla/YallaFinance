import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithSession as render } from "@/test/renderWithSession";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { NewDebtClient } from "./NewDebtClient";

const mocks = vi.hoisted(() => ({
  create: vi.fn(),
  push: vi.fn(),
  suggestions: vi.fn(),
  contracts: vi.fn(),
  movements: vi.fn(),
  categories: vi.fn(),
}));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("@/features/customers/api/customers.client", () => ({
  customersApi: {
    suggestCustomers: mocks.suggestions,
    listContracts: mocks.contracts,
    findFleetMovements: mocks.movements,
  },
}));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: mocks.categories },
}));
vi.mock("@/features/debts/api/debts.client", () => ({
  debtsApi: { createManualDebt: mocks.create },
}));
const customerId = "a30e2e53-51a7-f55f-8832-d9147e06bcc5";
const contractId = "22222222-2222-4222-8222-222222222222";
const categoryId = "33333333-3333-4333-8333-333333333333";
beforeEach(() => {
  vi.clearAllMocks();
  sessionStorage.clear();
  mocks.suggestions.mockResolvedValue([
    {
      id: customerId,
      customerId,
      name: "Cliente Silva",
      taxId: "12345678901",
      tenantId: "tenant-a",
    },
  ]);
  mocks.contracts.mockResolvedValue({
    items: [
      { id: contractId, number: "CT 123", plate: "ABC1D23", product: "TRUST", fleet: "Matriz" },
    ],
    page: 1,
    pageSize: 20,
    hasMore: false,
  });
  mocks.movements.mockResolvedValue([
    {
      id: "movement-1",
      customerId,
      tenantId: "tenant-a",
      occurrenceDate: "2020-01-01",
      contract: "CT 123",
      plate: "ABC1D23",
      product: "TRUST",
      fleet: "Matriz",
    },
  ]);
  mocks.categories.mockResolvedValue([{ id: categoryId, name: "Multa" }]);
  mocks.create.mockResolvedValue({ debtId: "44444444-4444-4444-8444-444444444444" });
});
afterEach(cleanup);

async function fill() {
  fireEvent.change(await screen.findByRole("combobox", { name: /Locatário/ }), {
    target: { value: "Silva" },
  });
  fireEvent.click(await screen.findByRole("option", { name: /Cliente Silva/ }));
  expect(mocks.contracts).not.toHaveBeenCalled();
  fireEvent.change(screen.getByLabelText(/Data da ocorrência/), {
    target: { value: "2020-01-01" },
  });
  await waitFor(() =>
    expect(screen.getByRole("combobox", { name: /Contrato/ })).not.toHaveAttribute(
      "aria-disabled",
      "true",
    ),
  );
  fireEvent.mouseDown(screen.getByRole("combobox", { name: /Contrato/ }));
  fireEvent.click(await screen.findByRole("option", { name: /CT 123/ }));
  fireEvent.mouseDown(screen.getByRole("combobox", { name: /Categoria/ }));
  fireEvent.click(await screen.findByRole("option", { name: "Multa" }));
  fireEvent.change(screen.getByRole("textbox", { name: /Valor/ }), { target: { value: "123,45" } });
  fireEvent.change(screen.getByLabelText(/Data da ocorrência/), {
    target: { value: "2020-01-01" },
  });
  fireEvent.change(screen.getByLabelText(/Descrição/), { target: { value: "Multa manual" } });
}
it("requires due date and submits only a manual debit using persisted customer lookup", async () => {
  render(<NewDebtClient />);
  expect(await screen.findByLabelText(/Vencimento/)).toBeRequired();
  expect(mocks.suggestions).not.toHaveBeenCalled();
  await fill();
  fireEvent.click(screen.getByRole("button", { name: "Salvar débito" }));
  await screen.findByText("Informe um vencimento válido");
  expect(mocks.create).not.toHaveBeenCalled();
  fireEvent.change(screen.getByLabelText(/Vencimento/), { target: { value: "2030-01-01" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar débito" }));
  await screen.findByText("Débito cadastrado em aberto.");
  expect(mocks.push).not.toHaveBeenCalled();
  expect(screen.getByRole("combobox", { name: /Locatário/ })).toHaveValue("");
  expect(screen.getByLabelText(/Descrição/)).toHaveValue("");
  expect(screen.getByLabelText(/Vencimento/)).toHaveValue("");
  expect(screen.getByLabelText(/Data da ocorrência/)).toHaveValue("");
  expect(
    Object.keys(sessionStorage).filter((key) => key.startsWith("manual-debt-attempt:")),
  ).toHaveLength(0);
  expect(mocks.create).toHaveBeenCalledWith(
    expect.objectContaining({
      customerId,
      contractId,
      categoryId,
      amount: "123.45",
      dueDate: "2030-01-01",
      idempotencyKey: expect.any(String),
    }),
    expect.any(Object),
  );
  expect(mocks.suggestions).toHaveBeenCalledWith("Silva", expect.any(Object), "persisted");
}, 15000);
it("locks an uncertain attempt and retries with the same key and payload", async () => {
  mocks.create.mockRejectedValueOnce(new Error("connection lost"));
  render(<NewDebtClient />);
  await screen.findByLabelText(/Vencimento/);
  await fill();
  fireEvent.change(screen.getByLabelText(/Vencimento/), { target: { value: "2030-01-01" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar débito" }));
  await screen.findByText(/Não foi possível confirmar/);
  expect(screen.getByLabelText(/Descrição/)).toBeDisabled();
  const first = mocks.create.mock.calls[0][0];
  fireEvent.click(screen.getByRole("button", { name: "Confirmar tentativa" }));
  await waitFor(() => expect(mocks.create).toHaveBeenCalledTimes(2));
  expect(mocks.create.mock.calls[1][0]).toEqual(first);
  await screen.findByText("Débito cadastrado em aberto.");
  expect(screen.getByLabelText(/Descrição/)).toBeEnabled();
  expect(screen.getByLabelText(/Descrição/)).toHaveValue("");
  expect(screen.getByRole("button", { name: "Salvar débito" })).toBeEnabled();
}, 15000);

it("clears the contract when the occurrence date changes and hides incompatible movements", async () => {
  render(<NewDebtClient />);
  await screen.findByLabelText(/Vencimento/);
  await fill();
  expect(screen.getByRole("combobox", { name: /Contrato/ })).toHaveTextContent("CT 123");
  fireEvent.change(screen.getByLabelText(/Data da ocorrência/), {
    target: { value: "2021-01-01" },
  });
  await waitFor(() =>
    expect(mocks.movements).toHaveBeenCalledWith(customerId, "2021-01-01", expect.any(Object)),
  );
  await waitFor(() =>
    expect(screen.getByRole("combobox", { name: /Contrato/ })).not.toHaveAttribute(
      "aria-disabled",
      "true",
    ),
  );
  expect(screen.getByRole("combobox", { name: /Contrato/ })).not.toHaveTextContent("CT 123");
  fireEvent.mouseDown(screen.getByRole("combobox", { name: /Contrato/ }));
  expect(screen.queryByRole("option", { name: /CT 123/ })).not.toBeInTheDocument();
}, 15000);
