import { cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ToastProvider } from "@/providers/ToastProvider";
import { companyApi } from "../company.client";
import type { CompanyConnectionDetail, CompanyConnectionSummary, CompanyPage } from "../types";
import { SettingsPage } from "./SettingsPage";

vi.mock("../company.client", () => ({
  companyApi: { getSettings: vi.fn(), saveSettings: vi.fn(), listConnections: vi.fn(), getConnection: vi.fn(),
    createConnection: vi.fn(), saveConnection: vi.fn(), saveConnectionSettings: vi.fn(), saveMapping: vi.fn(),
    removeMapping: vi.fn(), savePaymentMethod: vi.fn(), removePaymentMethod: vi.fn(),
    listCategories: vi.fn(), listPaymentMethods: vi.fn() },
}));
afterEach(() => { cleanup(); vi.resetAllMocks(); });

const emptyPage = <T,>(): CompanyPage<T> => ({ items: [], page: 1, pageSize: 20, totalItems: 0 });
const connection: CompanyConnectionSummary = {
  id: "10000000-0000-4000-8000-000000000001", name: "Conexão de teste",
  provider: "Nibo", group: "FinancialSystem", enabled: false,
  createdAt: "2026-01-01T00:00:00Z", updatedAt: "2026-01-02T00:00:00Z",
};
function detail(): CompanyConnectionDetail {
  return {
    connection, providerSupported: true,
    settings: [
      { group: "Connection", key: "BaseUrl", value: "https://unit.example.test", configured: true, sensitive: false },
      { group: "Connection", key: "CompanyId", value: null, configured: false, sensitive: false },
      { group: "Authentication", key: "ApiToken", value: "persisted-token", configured: true, sensitive: true },
    ],
    categoryMappings: { ...emptyPage(), totalItems: 21, items: [{
      id: "mapping-test", mappingType: "Category", internalValue: "category-test",
      internalName: "Categoria inativa de teste", internalActive: false, externalValue: "external-test",
      createdAt: "2026-01-01T00:00:00Z", updatedAt: "2026-01-01T00:00:00Z",
    }] },
    receiptAccountMappings: emptyPage(), paymentMethods: emptyPage(),
  };
}
beforeEach(() => {
  vi.mocked(companyApi.getSettings).mockImplementation(async (group) => {
    const values = {
      payments: { maximumLinkDays: "45", publicOrigin: null },
      session: { sessionLifetimeMinutes: null },
      "user-registration": { initialPassword: "persisted-initial-password" },
      "financial-settlement": { receiptTimeZoneId: null },
      "job-retry": { limits: { FINANCIAL_CHARGE_CREATE_V1: 0, FINANCIAL_CHARGE_SETTLE_V1: 0, FINANCIAL_DEBIT_INBOUND_V1: 0 } },
    };
    return values[group];
  });
  vi.mocked(companyApi.listConnections).mockResolvedValue({ ...emptyPage(), items: [connection], totalItems: 1 });
  vi.mocked(companyApi.getConnection).mockResolvedValue(detail());
  vi.mocked(companyApi.saveSettings).mockResolvedValue("saved");
  vi.mocked(companyApi.createConnection).mockResolvedValue("20000000-0000-4000-8000-000000000002");
  vi.mocked(companyApi.saveConnection).mockResolvedValue("saved");
  vi.mocked(companyApi.saveConnectionSettings).mockResolvedValue("saved");
  vi.mocked(companyApi.saveMapping).mockResolvedValue("saved");
  vi.mocked(companyApi.removeMapping).mockResolvedValue("saved");
  vi.mocked(companyApi.savePaymentMethod).mockResolvedValue("saved");
  vi.mocked(companyApi.removePaymentMethod).mockResolvedValue("saved");
  vi.mocked(companyApi.listCategories).mockResolvedValue([]);
  vi.mocked(companyApi.listPaymentMethods).mockResolvedValue([]);
});
function renderSettings() {
  return renderWithSession(<ToastProvider><SettingsPage /></ToastProvider>);
}
async function openConnections() {
  await waitForIdle();
  fireEvent.click(screen.getByRole("tab", { name: "Conexões", hidden: true }));
  await screen.findByText(connection.name);
  await waitForIdle();
}
async function waitForIdle() {
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Consultando configurações..." })).not.toBeInTheDocument());
}
async function openNewConnection(provider: "Nibo" | "Sicoob" | "Asaas") {
  await openConnections();
  fireEvent.click(screen.getByRole("button", { name: "Nova conexão" }));
  fireEvent.click(screen.getByRole("menuitem", { name: new RegExp(provider) }));
}

it("opens the Asaas configuration with all fields even without any persisted values", async () => {
  renderSettings();
  await openNewConnection("Asaas");
  expect(screen.getByLabelText(/URL base/)).toHaveValue("");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(null);
  expect(screen.queryByLabelText(/Ambiente/)).not.toBeInTheDocument();
  expect(companyApi.getConnection).not.toHaveBeenCalled();
});

it("creates an Asaas connection then opens all empty parameters for its assigned id", async () => {
  const createdId = "20000000-0000-4000-8000-000000000002";
  const createdDetail = detail();
  createdDetail.connection = { ...connection, id: createdId, name: "Asaas nova", provider: "Asaas", group: "PaymentProvider" };
  createdDetail.settings = [];
  vi.mocked(companyApi.getConnection).mockResolvedValue(createdDetail);
  renderSettings();
  await openNewConnection("Asaas");
  fireEvent.change(screen.getByLabelText(/Nome da conexão/), { target: { value: "Asaas nova" } });
  fireEvent.click(screen.getByRole("button", { name: "Criar conexão" }));
  await screen.findByDisplayValue(createdId);
  expect(companyApi.createConnection).toHaveBeenCalledWith({ name: "Asaas nova", provider: "Asaas", group: "PaymentProvider", enabled: false }, expect.anything());
  expect(companyApi.getConnection).toHaveBeenCalledWith(createdId, expect.anything(), expect.anything());
  expect(screen.getByLabelText(/URL base/)).toHaveValue("");
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
  expect(screen.getByLabelText(/Timeout/)).toHaveValue(null);
});

it("loads every company setting group on screen entry and preserves persisted values", async () => {
  vi.mocked(companyApi.getSettings).mockResolvedValueOnce({ maximumLinkDays: "valor-invalido", publicOrigin: null });
  renderSettings();
  expect(companyApi.listConnections).not.toHaveBeenCalled();
  const days = await screen.findByDisplayValue("valor-invalido");
  expect(days).toHaveAttribute("aria-invalid", "true");
  expect(screen.getByLabelText(/Origem pública/)).toHaveValue("");
  expect(screen.getByLabelText("Senha inicial de usuários")).toHaveValue("persisted-initial-password");
  expect(screen.queryByLabelText("Identificador da empresa")).not.toBeInTheDocument();
  await waitFor(() => expect(companyApi.getSettings).toHaveBeenCalledTimes(4));
  expect(new Set(vi.mocked(companyApi.getSettings).mock.calls.map(([group]) => group))).toEqual(new Set([
    "payments", "session", "user-registration", "financial-settlement",
  ]));
  expect(screen.queryByRole("button", { name: /Consultar/ })).not.toBeInTheDocument();
  expect(screen.getAllByRole("button", { name: "Salvar" }).every((button) => button.hasAttribute("disabled"))).toBe(true);
});

it("shows the persisted initial password and missing fields remain empty", async () => {
  renderSettings();
  const password = await screen.findByLabelText("Senha inicial de usuários");
  await waitForIdle();
  expect(password).toHaveValue("persisted-initial-password");
  await waitFor(() => expect(companyApi.getSettings).toHaveBeenCalledTimes(4));
  expect(screen.getByLabelText(/Fuso horário padrão/)).toHaveValue("");
});

it("does not submit Payments when the required maximum link period is empty", async () => {
  renderSettings();
  await screen.findByDisplayValue("45");
  await waitForIdle();
  const maximumLinkDays = screen.getByLabelText(/Prazo máximo do link/);
  fireEvent.change(maximumLinkDays, { target: { value: "" } });

  expect(maximumLinkDays).toHaveAttribute("aria-invalid", "true");
  const paymentsPanel = screen.getByRole("heading", { name: "Pagamentos" }).closest("section");
  expect(within(paymentsPanel!).getByRole("button", { name: "Salvar" })).toBeDisabled();
  expect(companyApi.saveSettings).not.toHaveBeenCalled();
});

it("saves only the edited company settings group and preserves the draft on failure", async () => {
  renderSettings();
  await screen.findByDisplayValue("45");
  await waitForIdle();
  fireEvent.change(screen.getByLabelText(/Origem pública/), { target: { value: "https://empresa.example.test" } });
  vi.mocked(companyApi.saveSettings).mockRejectedValueOnce(new Error("unit-failure"));
  fireEvent.click(screen.getAllByRole("button", { name: "Salvar" })[0]);
  await waitFor(() => expect(companyApi.saveSettings).toHaveBeenCalledTimes(1));
  expect(companyApi.saveSettings).toHaveBeenCalledWith("payments", {
    maximumLinkDays: "45", publicOrigin: "https://empresa.example.test",
  }, expect.anything());
  expect(screen.getByLabelText(/Origem pública/)).toHaveValue("https://empresa.example.test");
  expect(await screen.findByText("unit-failure")).toBeInTheDocument();
  expect(companyApi.saveSettings).not.toHaveBeenCalledWith("session", expect.anything(), expect.anything());
});

it("shows the API validation message when the rejected error is response-shaped", async () => {
  renderSettings();
  await screen.findByDisplayValue("45");
  await waitForIdle();
  fireEvent.change(screen.getByLabelText(/Origem pública/), { target: { value: "https://empresa.example.test" } });
  vi.mocked(companyApi.saveSettings).mockRejectedValueOnce({
    response: { data: { errors: [{ message: "Falha de validação da API." }] } },
  });
  fireEvent.click(screen.getAllByRole("button", { name: "Salvar" })[0]);

  expect(await screen.findByText("Falha de validação da API.")).toBeInTheDocument();
});

it("loads a connection once and reuses its aggregate detail when switching tabs", async () => {
  renderSettings();
  await openConnections();
  expect(companyApi.getConnection).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: /Abrir conexão/ }));
  await screen.findByDisplayValue("https://unit.example.test");
  await waitForIdle();
  expect(screen.getByLabelText(/Nome da conexão/)).toHaveValue(connection.name);
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("persisted-token");
  expect(screen.getByRole("switch", { name: "Conexão ativa" })).not.toBeChecked();
  fireEvent.click(screen.getByRole("tab", { name: "Categorias" }));
  expect(await screen.findByText(/Categoria inativa de teste/)).toBeVisible();
  expect(screen.queryByLabelText("Conexão financeira")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("tab", { name: "Configuração", hidden: true }));
  expect(companyApi.getConnection).toHaveBeenCalledTimes(1);
});

it("preserves unsaved credentials while the aggregate lists are paginated", async () => {
  renderSettings();
  await openConnections();
  fireEvent.click(screen.getByRole("button", { name: /Abrir conexão/ }));
  await screen.findByDisplayValue("https://unit.example.test");
  await waitForIdle();
  fireEvent.change(screen.getByLabelText(/Token de API/), { target: { value: "unit-only-draft" } });
  fireEvent.click(screen.getByRole("tab", { name: "Categorias" }));
  const pagination = screen.getByLabelText("Paginação de Categorias mapeadas");
  fireEvent.click(within(pagination).getByRole("button", { name: "Go to page 2", hidden: true }));
  await waitFor(() => expect(companyApi.getConnection).toHaveBeenCalledTimes(2));
  await waitForIdle();
  fireEvent.click(screen.getByRole("tab", { name: "Configuração", hidden: true }));
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("unit-only-draft");
});

it("retries only the failing group and releases the blocking overlay on failure", async () => {
  vi.mocked(companyApi.getSettings).mockRejectedValueOnce(new Error("unit-failure"));
  renderSettings();
  const retry = await screen.findByRole("button", { name: "Tentar novamente Pagamentos" });
  await waitFor(() => expect(screen.queryByRole("status", { name: "Consultando configurações..." })).not.toBeInTheDocument());
  fireEvent.click(retry);
  await screen.findByDisplayValue(45);
  expect(companyApi.getSettings).toHaveBeenCalledTimes(5);
  expect(companyApi.listConnections).not.toHaveBeenCalled();
});

it("opens an empty Sicoob form without fetching a fictitious connection", async () => {
  renderSettings();
  await openNewConnection("Sicoob");
  expect(screen.getByLabelText("Senha da chave privada")).toHaveValue("");
  expect(screen.getByLabelText(/Certificado PEM/)).toHaveValue("");
  expect(screen.getByRole("tab", { name: "Formas de pagamento" })).toBeInTheDocument();
  expect(companyApi.getConnection).not.toHaveBeenCalled();
});

it("confirms discarding a new connection draft before returning to the list", async () => {
  renderSettings();
  await openNewConnection("Nibo");
  fireEvent.change(screen.getByLabelText(/Token de API/), { target: { value: "unit-only-draft" } });
  fireEvent.click(screen.getByRole("button", { name: "Voltar para conexões" }));
  expect(screen.getByRole("dialog", { name: "Descartar alterações?" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Continuar editando" }));
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("unit-only-draft");
  fireEvent.click(screen.getByRole("button", { name: "Voltar para conexões" }));
  fireEvent.click(screen.getByRole("button", { name: "Descartar alterações" }));
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  fireEvent.click(screen.getByRole("button", { name: "Nova conexão" }));
  fireEvent.click(screen.getByRole("menuitem", { name: /Nibo/ }));
  expect(screen.getByLabelText(/Token de API/)).toHaveValue("");
}, 15_000);
