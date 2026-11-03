import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithSession as render } from "@/test/renderWithSession";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ChargeReviewClient } from "@/features/charges/components/ChargeReviewClient";
import { getChargeToday } from "@/features/charges/businessRules";

const modes = vi.hoisted(() => ({ selected: false, singlePaymentMethod: false, setChargeIntent: vi.fn() }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
  useSearchParams: () => new URLSearchParams(modes.selected
    ? "source=selected&debtIds=debt-selected"
    : "source=standalone"),
}));
vi.mock("@/providers/ToastProvider", () => {
  const showToast = vi.fn();
  return { useToast: () => ({ showToast }) };
});
vi.mock("@/features/customers/api/customers.client", () => ({
  customersApi: { findCustomers: async () => [] },
}));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: async () => [{ id: "fine", name: "Multa" }] },
}));
vi.mock("@/features/catalogs/api/payment-methods.client", () => ({
  paymentMethodsApi: {
    listPaymentMethods: async () => modes.singlePaymentMethod
      ? [{ id: "first", code: "Pix", name: "PIX A" }]
      : [
          { id: "first", code: "Pix", name: "PIX A" },
          { id: "selected", code: "Pix", name: "PIX B" },
        ],
  },
}));
vi.mock("@/features/debts/api/debts.client", () => ({
  debtsApi: { getDebts: async () => modes.selected ? [{
    id: "debt-selected",
    category: "Multa",
    categoryId: "fine",
    tenantId: "tenant",
    customerId: "customer",
    tenantName: "João",
    contract: "CT 10432",
    product: "TRUST",
    plate: "DEF4G56",
    fleet: "Frota",
    occurrenceDate: "2026-09-24",
    amount: 100,
    status: "Open",
    description: "MULTA | Teste | CT 10432",
    source: "Yalla Digital",
  }] : [] },
}));
vi.mock("@/features/charges/context/ChargeDraftProvider", () => {
  const state = {
    debts: [],
    activeDraft: {
      source: "standalone",
      customerId: "customer",
      customerName: "João",
      standalone: {
        category: "Multa",
        categoryId: "fine",
        amount: 100,
        occurrenceDate: "2026-09-24",
        descriptionDetail: "Multa",
        dueDate: "2026-09-29",
        customerNote: "",
        paymentMethodId: "selected",
      },
      movement: {
        tenantId: "tenant",
        plate: "DEF4G56",
        contract: "Contrato",
        product: "TRUST",
        fleet: "Frota",
        costCenter: "Centro",
      },
    },
  };
  return { useChargeDraft: () => ({ draft: state.activeDraft, setChargeIntent: modes.setChargeIntent }) };
});

afterEach(cleanup);
beforeEach(() => {
  modes.selected = false;
  modes.singlePaymentMethod = false;
  modes.setChargeIntent.mockClear();
});

it.each([false, true])("reviews charges with selected debts=%s without selecting a payment method", async (selected) => {
  modes.selected = selected;
  const user = userEvent.setup();
  render(<ChargeReviewClient />);
  const dueDate = await screen.findByLabelText("Vencimento");
  expect(screen.queryByRole("combobox", { name: "Forma de pagamento" })).not.toBeInTheDocument();
  fireEvent.change(dueDate, { target: { value: getChargeToday() } });
  await user.click(screen.getByRole("button", { name: "Gerar cobrança" }));
  await waitFor(() => expect(modes.setChargeIntent).toHaveBeenCalledWith(
    expect.objectContaining(selected
      ? { payment: expect.objectContaining({ dueDate: getChargeToday() }) }
      : { standalone: expect.objectContaining({ dueDate: getChargeToday() }) }),
  ));
});
