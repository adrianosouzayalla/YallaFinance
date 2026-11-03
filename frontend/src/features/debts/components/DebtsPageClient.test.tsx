import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithSession as render } from "@/test/renderWithSession";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { DebtsPageClient } from "@/features/debts/components/DebtsPageClient";
import type { Debt, DebtFilters } from "@/features/debts/types/debt.types";

const mocks = vi.hoisted(() => ({ listDebts: vi.fn(), listCategories: vi.fn(), toast: vi.fn() }));
vi.mock("@/features/debts/api/debts.client", () => ({ debtsApi: { listDebts: mocks.listDebts } }));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: mocks.listCategories },
}));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast: mocks.toast }) }));
beforeEach(() => {
  vi.clearAllMocks();
  sessionStorage.clear();
  mocks.listCategories.mockResolvedValue([]);
  mocks.listDebts.mockResolvedValue({
    items: [],
    totalItems: 0,
    page: 1,
    pageSize: 5,
    summary: { openCount: 0, openAmount: 0, chargingAmount: 0, paidAmount: 0 },
  });
});
it("restores filters when returning from the manual debit form", async () => {
  sessionStorage.setItem(
    'debt-filters:["tenant-test","session-test"]',
    JSON.stringify({
      categories: [],
      status: "Open",
      startDate: "2020-01-01",
      endDate: "2030-01-01",
      sortDirection: "asc",
      page: 2,
      pageSize: 5,
    }),
  );
  window.history.replaceState({}, "", "/debts");
  render(<DebtsPageClient />);
  await waitFor(() =>
    expect(mocks.listDebts).toHaveBeenCalledWith(
      expect.objectContaining({ status: "Open", page: 2, startDate: "2020-01-01" }),
      expect.any(Object),
    ),
  );
});
afterEach(() => {
  cleanup();
  window.history.replaceState({}, "", "/");
});

it.each(["Em aberto", "Desconhecido", "1", ""])(
  "rejects invalid URL status %s without listing every debt",
  async (status) => {
    window.history.replaceState({}, "", `/debts?status=${encodeURIComponent(status)}`);
    render(<DebtsPageClient />);
    expect(await screen.findByText(/Status da URL inválido/)).toBeVisible();
    expect(mocks.listDebts).not.toHaveBeenCalled();
  },
);

it.each(["Open", "InCollection", "Paid", "Canceled"])(
  "sends technical status %s to the API",
  async (status) => {
    window.history.replaceState({}, "", `/debts?status=${status}`);
    render(<DebtsPageClient />);
    await waitFor(() =>
      expect(mocks.listDebts).toHaveBeenCalledWith(
        expect.objectContaining({ status }),
        expect.objectContaining({ signal: expect.any(AbortSignal), contextId: "session-test" }),
      ),
    );
  },
);

it("allows explicit recovery from an invalid URL by clearing the filters", async () => {
  window.history.replaceState({}, "", "/debts?status=Unknown");
  render(<DebtsPageClient />);
  expect(await screen.findByText(/Status da URL inválido/)).toBeVisible();
  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  await waitFor(() =>
    expect(mocks.listDebts).toHaveBeenCalledWith(
      expect.objectContaining({ status: "Todos" }),
      expect.objectContaining({ signal: expect.any(AbortSignal), contextId: "session-test" }),
    ),
  );
  expect(new URLSearchParams(window.location.search).has("status")).toBe(false);
  expect(screen.queryByText(/Status da URL inválido/)).not.toBeInTheDocument();
});

function paginatedDebts(customerOnSecondPage = "customer-a") {
  const base: Debt = {
    id: "debt-a",
    category: "Multa",
    categoryId: "fine",
    tenantId: "tenant-test",
    customerId: "customer-a",
    tenantName: "João",
    contract: "CT 1",
    product: "TRUST",
    plate: "ABC1D23",
    fleet: "Frota",
    occurrenceDate: "2026-10-01",
    amount: 10,
    status: "Open",
    description: "Multa",
    source: "Yalla Digital",
  };
  mocks.listDebts.mockImplementation(async (filters: DebtFilters) => ({
    items:
      filters.page === 2
        ? [
            {
              ...base,
              id: "debt-b",
              category: "Avaria",
              amount: 20,
              customerId: customerOnSecondPage,
            },
          ]
        : [base],
    totalItems: 6,
    page: filters.page,
    pageSize: 5,
    summary: { openCount: 6, openAmount: 30, chargingAmount: 0, paidAmount: 0 },
  }));
}

async function selectFirstPage() {
  const first = await screen.findByRole("checkbox", { name: "Selecionar débito Multa de João" });
  await waitFor(() => expect(first).toBeEnabled());
  fireEvent.click(first);
  expect(screen.getByText("1 débito selecionado")).toBeVisible();
}

async function nextPage() {
  fireEvent.click(screen.getByRole("button", { name: /page 2/i }));
  const second = await screen.findByRole("checkbox", { name: "Selecionar débito Avaria de João" });
  await waitFor(() => expect(second).toBeEnabled());
  return second;
}

it("keeps selections from different pages in the count, total and charge review link", async () => {
  paginatedDebts();
  render(<DebtsPageClient />);
  await selectFirstPage();
  const second = await nextPage();
  expect(screen.getByText("1 débito selecionado")).toBeVisible();
  fireEvent.click(second);
  expect(screen.getByText("2 débitos selecionados")).toBeVisible();
  expect(screen.getByText(/Total: R\$\s*30,00/)).toBeVisible();
  expect(screen.getByRole("link", { name: "Cobrar selecionados" })).toHaveAttribute(
    "href",
    "/charges/review?source=selected&debtIds=debt-a,debt-b",
  );
  fireEvent.click(screen.getByRole("button", { name: /page 1/i }));
  expect(
    await screen.findByRole("checkbox", { name: "Selecionar débito Multa de João" }),
  ).toBeChecked();
});

it("deselects only the current page when toggling its header checkbox", async () => {
  paginatedDebts();
  render(<DebtsPageClient />);
  await selectFirstPage();
  await nextPage();
  const header = screen.getByRole("checkbox", { name: "Selecionar todos os débitos" });
  fireEvent.click(header);
  expect(screen.getByText("2 débitos selecionados")).toBeVisible();
  fireEvent.click(header);
  expect(screen.getByText("1 débito selecionado")).toBeVisible();
  expect(screen.getByRole("link", { name: "Cobrar selecionados" })).toHaveAttribute(
    "href",
    "/charges/review?source=selected&debtIds=debt-a",
  );
});

it("validates the customer across pages instead of validating only visible debts", async () => {
  paginatedDebts("customer-b");
  render(<DebtsPageClient />);
  await selectFirstPage();
  fireEvent.click(await nextPage());
  expect(screen.getByText("2 débitos selecionados")).toBeVisible();
  expect(screen.getByRole("button", { name: "Cobrar selecionados" })).toBeDisabled();
  expect(screen.getByText("Selecione débitos de um único locatário")).toBeVisible();
});

it("preserves selection when sorting and clears it when applying filters", async () => {
  paginatedDebts();
  render(<DebtsPageClient />);
  await selectFirstPage();
  fireEvent.click(screen.getByText("Valor", { selector: ".MuiTableSortLabel-root" }));
  await waitFor(() =>
    expect(screen.getByRole("checkbox", { name: "Selecionar débito Multa de João" })).toBeEnabled(),
  );
  expect(screen.getByText("1 débito selecionado")).toBeVisible();
  expect(screen.getByRole("checkbox", { name: "Selecionar débito Multa de João" })).toBeChecked();
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  await waitFor(() => expect(screen.queryByText("1 débito selecionado")).not.toBeInTheDocument());
});

it("lets the user clear all selected pages explicitly", async () => {
  paginatedDebts();
  render(<DebtsPageClient />);
  await selectFirstPage();
  fireEvent.click(await nextPage());
  fireEvent.click(screen.getByRole("button", { name: "Limpar seleção" }));
  expect(screen.queryByText("2 débitos selecionados")).not.toBeInTheDocument();
  expect(
    screen.getByRole("checkbox", { name: "Selecionar débito Avaria de João" }),
  ).not.toBeChecked();
});
