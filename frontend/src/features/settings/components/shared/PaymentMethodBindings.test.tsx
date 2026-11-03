import { cleanup, fireEvent, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { DEFAULT_DETAIL_FILTERS, type CompanyConnectionDetail } from "../../types";
import { PaymentMethodBindings } from "./PaymentMethodBindings";

const mutations = vi.hoisted(() => ({ save: { isPending: false, mutateAsync: vi.fn() }, remove: { isPending: false, mutateAsync: vi.fn() } }));
vi.mock("../../hooks/useConnectionAdminMutations", () => ({ usePaymentMethodMutations: () => mutations }));
afterEach(() => { cleanup(); vi.clearAllMocks(); });
const methodId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const reportDirty = vi.fn();
function detail(): CompanyConnectionDetail {
  const page = { items: [], page: 1, pageSize: 20, totalItems: 0 };
  return {
    connection: { id: "connection", provider: "Asaas", group: "PaymentProvider", enabled: false, name: "Asaas", createdAt: "", updatedAt: null },
    providerSupported: true, settings: [], categoryMappings: page, receiptAccountMappings: page, paymentMethods: page,
    supportedPaymentInstruments: ["CreditCard"],
    availablePaymentMethods: [{ id: methodId, code: "opaque", name: "Cartão", active: true,
      instrumentType: null, internalDescription: "Cartão Asaas", canClassify: true }],
  };
}
function renderBindings(value?: CompanyConnectionDetail) {
  return renderWithSession(<ToastProvider><PaymentMethodBindings id={value ? "connection" : null} detail={value}
    filters={DEFAULT_DETAIL_FILTERS} onFilters={vi.fn()} reportBusy={vi.fn()} reportDirty={reportDirty} /></ToastProvider>);
}

it("keeps every empty field visible without configuration or invented values", () => {
  renderBindings();
  expect(screen.getByLabelText("Forma de pagamento")).toBeInTheDocument();
  expect(screen.getByLabelText("Modalidade")).toBeInTheDocument();
  expect(screen.getByLabelText("Descrição interna")).toHaveValue("");
  expect(screen.getByRole("button", { name: "Salvar vínculo" })).toBeDisabled();
});

it("uses server capabilities and internal labels and saves a typed administrative binding", async () => {
  renderBindings(detail());
  fireEvent.mouseDown(screen.getByLabelText("Forma de pagamento"));
  fireEvent.click(await screen.findByRole("option", { name: "Cartão — Cartão Asaas" }));
  fireEvent.mouseDown(screen.getByLabelText("Modalidade"));
  expect(screen.queryByRole("option", { name: "Pix" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("option", { name: "Cartão de crédito" }));
  fireEvent.change(screen.getByLabelText("Descrição interna"), { target: { value: "Conta principal" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar vínculo" }));
  await screen.findByText("Vínculo de pagamento salvo.");
  expect(mutations.save.mutateAsync).toHaveBeenCalledWith({ linkId: null, draft: {
    paymentMethodId: methodId, enabled: false, instrumentType: "CreditCard", internalDescription: "Conta principal",
  } });
  expect(reportDirty).toHaveBeenLastCalledWith("payment-method-binding", false);
});

it("edits description of a historical unclassified link while keeping its identity", async () => {
  const value = detail();
  value.paymentMethods = { items: [{ id: "link", paymentMethodId: methodId, paymentMethodName: "Pix público",
    paymentMethodActive: true, enabled: true, instrumentType: null, internalDescription: "Conta antiga", createdAt: "", updatedAt: null }],
    page: 1, pageSize: 20, totalItems: 1 };
  renderBindings(value);
  fireEvent.click(screen.getByRole("button", { name: "Editar" }));
  expect(screen.getByLabelText("Forma de pagamento")).toHaveAttribute("aria-disabled", "true");
  fireEvent.change(screen.getByLabelText("Descrição interna"), { target: { value: "Conta principal" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar vínculo" }));
  await screen.findByText("Vínculo de pagamento salvo.");
  expect(mutations.save.mutateAsync).toHaveBeenCalledWith({ linkId: "link", draft: {
    paymentMethodId: methodId, enabled: true, instrumentType: null, internalDescription: "Conta principal",
  } });
});
