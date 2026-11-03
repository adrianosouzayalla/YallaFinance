import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { companyApi } from "../../company.client";
import type { CompanyConnectionDetail } from "../../types";
import { AsaasConnectionScreen } from "./AsaasConnectionScreen";

vi.mock("../../company.client", () => ({ companyApi: {
  getConnection: vi.fn(), saveConnectionSettings: vi.fn(), createConnection: vi.fn(), saveConnection: vi.fn(),
} }));
afterEach(() => { cleanup(); vi.resetAllMocks(); });
const connectionId = "44444444-4444-4444-8444-444444444444";
function detail(): CompanyConnectionDetail {
  const emptyPage = { items: [], page: 1, pageSize: 20, totalItems: 0 };
  return {
    connection: { id: connectionId, name: "Asaas da empresa", provider: "Asaas", group: "PaymentProvider", enabled: false,
      createdAt: "2026-01-01T00:00:00Z", updatedAt: null },
    providerSupported: true, settings: [], categoryMappings: emptyPage, receiptAccountMappings: emptyPage, paymentMethods: emptyPage,
  };
}
beforeEach(() => {
  vi.mocked(companyApi.getConnection).mockResolvedValue(detail());
  vi.mocked(companyApi.saveConnectionSettings).mockResolvedValue(connectionId);
});
const reportBusy = vi.fn();
const reportDirty = vi.fn();
function renderAsaas(id: string | null = connectionId) {
  return renderWithSession(<ToastProvider><AsaasConnectionScreen id={id} reportBusy={reportBusy}
    reportDirty={reportDirty} onCreated={vi.fn()} /></ToastProvider>);
}
async function enterParameters() {
  await screen.findByDisplayValue("Asaas da empresa");
  fireEvent.change(screen.getByLabelText(/URL base/), { target: { value: "https://api-sandbox.asaas.com/v3" } });
  fireEvent.change(screen.getByLabelText(/Token de API/), { target: { value: "unit-token" } });
  fireEvent.change(screen.getByLabelText(/Timeout/), { target: { value: "60" } });
}

it("renders every supported field when the database has no parameter rows", async () => {
  renderAsaas();
  await screen.findByDisplayValue("Asaas da empresa");
  expect(screen.getByLabelText(/URL base/)).toHaveValue("");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(null);
  expect(screen.getByText(/api-sandbox.asaas.com/)).toBeInTheDocument();
  expect(screen.getByText(/60 a 300 segundos/)).toBeInTheDocument();
  expect(screen.queryByLabelText(/Ambiente/)).not.toBeInTheDocument();
  expect(screen.getByRole("tab", { name: "Formas de pagamento" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Salvar parâmetros do provider" })).toBeDisabled();
});

it("shows empty fields before creating the connection without querying a fabricated id", () => {
  renderAsaas(null);
  expect(screen.getByLabelText(/URL base/)).toHaveValue("");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(null);
  expect(companyApi.getConnection).not.toHaveBeenCalled();
  expect(screen.getByRole("button", { name: "Salvar parâmetros do provider" })).toBeDisabled();
});

it("loads persisted values and submits exactly the four canonical keys", async () => {
  const persisted = detail();
  persisted.settings = [
    { group: "Connection", key: "BaseUrl", value: "https://api.asaas.com/v3", configured: true, sensitive: false },
    { group: "Authentication", key: "ApiToken", value: "persisted-unit-token", configured: true, sensitive: true },
    { group: "Transport", key: "RequestTimeoutSeconds", value: "120", configured: true, sensitive: false },
  ];
  vi.mocked(companyApi.getConnection).mockResolvedValue(persisted);
  renderAsaas();
  await screen.findByDisplayValue("persisted-unit-token");
  expect(screen.getByLabelText(/URL base/)).toHaveValue("https://api.asaas.com/v3");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(120);
  fireEvent.change(screen.getByLabelText(/Timeout/), { target: { value: "180" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar parâmetros do provider" }));
  await screen.findByText("Configurações do provider salvas.");
  expect(companyApi.saveConnectionSettings).toHaveBeenCalledWith(connectionId, [
    { group: "Connection", key: "BaseUrl", value: "https://api.asaas.com/v3" },
    { group: "Authentication", key: "ApiToken", value: "persisted-unit-token" },
    { group: "Pix", key: "ReceivedLookbackDays", value: "" },
    { group: "Transport", key: "RequestTimeoutSeconds", value: "180" },
  ], expect.anything());
});

it("cancels parameter drafts back to the missing persisted values", async () => {
  renderAsaas();
  await enterParameters();
  fireEvent.click(screen.getByRole("button", { name: "Cancelar parâmetros" }));
  expect(screen.getByLabelText(/URL base/)).toHaveValue("");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(null);
  expect(companyApi.saveConnectionSettings).not.toHaveBeenCalled();
  expect(reportDirty).toHaveBeenCalledWith("connection-settings", false);
});

it("preserves the draft and releases mutation busy on failure", async () => {
  vi.mocked(companyApi.saveConnectionSettings).mockRejectedValue(new Error("Confira o token informado."));
  renderAsaas();
  await enterParameters();
  fireEvent.click(screen.getByRole("button", { name: "Salvar parâmetros do provider" }));
  await screen.findByText("Confira o token informado.");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("unit-token");
  await waitFor(() => expect(screen.getByRole("button", { name: "Salvar parâmetros do provider" })).not.toBeDisabled());
  expect(reportBusy).toHaveBeenCalledWith(`save-provider-settings-${connectionId}`, false);
});
