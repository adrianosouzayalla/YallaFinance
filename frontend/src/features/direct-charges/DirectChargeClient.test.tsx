import {
  act,
  cleanup,
  fireEvent,
  render as renderRaw,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClientProvider } from "@tanstack/react-query";
import { SessionProvider } from "@/features/auth/context/SessionProvider";
import { createQueryClient } from "@/providers/QueryProvider";
import { renderWithSession as render } from "@/test/renderWithSession";
import {
  directCategories,
  directCategoryId,
  directCustomers,
  directCostCenters,
  directChargeReceipt,
  createDirectChargeResult,
} from "@/test/directChargeFixtures";
import { DirectChargeClient, DirectChargeSuccessClient } from "./DirectChargeClient";
import { ApiError } from "@/services/api/api-error";
import userEvent from "@testing-library/user-event";

const mocks = vi.hoisted(() => ({
  submit: vi.fn(),
  customers: vi.fn(),
  centers: vi.fn(),
  categories: vi.fn(),
  toast: vi.fn(),
  refreshStatus: vi.fn(),
  replace: vi.fn(),
}));
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: mocks.replace }) }));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast: mocks.toast }) }));
vi.mock("@/features/charges/api/charges.client", () => ({
  chargesApi: { submitDirectCharge: mocks.submit, refreshDirectChargeStatus: mocks.refreshStatus },
}));
vi.mock("@/features/catalogs/api/financial-catalog.client", () => ({
  financialCatalogApi: { findCustomersByDocument: mocks.customers, listCostCenters: mocks.centers },
}));
vi.mock("@/features/catalogs/api/categories.client", () => ({
  categoriesApi: { listCategories: mocks.categories },
}));
beforeEach(() => {
  vi.resetAllMocks();
  vi.stubGlobal(
    "ResizeObserver",
    class {
      observe() {}
      unobserve() {}
      disconnect() {}
    },
  );
  mocks.submit.mockImplementation(async (payload) => createDirectChargeResult(payload));
  mocks.refreshStatus.mockResolvedValue(directChargeReceipt);
  mocks.customers.mockResolvedValue(directCustomers);
  mocks.centers.mockResolvedValue(directCostCenters);
  mocks.categories.mockResolvedValue([
    ...directCategories,
    { ...directCategories[0], id: "inactive", name: "Categoria inativa", active: false },
  ]);
  vi.stubGlobal(
    "fetch",
    vi.fn(() => {
      throw new Error("Unexpected real network");
    }),
  );
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

async function ready() {
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
}
async function expectError(message: string | RegExp) {
  await waitFor(() =>
    expect(mocks.toast).toHaveBeenCalledWith({
      message: typeof message === "string" ? message : expect.stringMatching(message),
      severity: "error",
    }),
  );
  await ready();
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
}
function choose(label: RegExp, option: string) {
  fireEvent.mouseDown(screen.getByRole("combobox", { name: label }));
  fireEvent.click(screen.getByRole("option", { name: option }));
}
async function fillBasics(contract = "CT-TEST") {
  await ready();
  fireEvent.change(screen.getByLabelText(/CPF\/CNPJ/), { target: { value: "12345678901" } });
  fireEvent.click(screen.getByRole("button", { name: "Consultar" }));
  await waitFor(() => expect(screen.getByLabelText("Nome")).toHaveValue("Cliente retornado"));
  await ready();
  fireEvent.change(screen.getByLabelText(/Contrato/), { target: { value: contract } });
  fireEvent.change(screen.getByLabelText(/Placa/), { target: { value: "ABC1D23" } });
  fireEvent.change(screen.getByLabelText(/Vencimento/), { target: { value: "2030-10-30" } });
  choose(/Centro de custo/, "Operação do catálogo");
}
async function openDebts() {
  await fillBasics();
  fireEvent.click(screen.getByRole("button", { name: "Adicionar débitos" }));
}
function fillDebt(detail = "Detalhe informado", amount = "150,00") {
  choose(/Categoria Yalla/, "Multa do banco");
  fireEvent.change(screen.getByLabelText(/Ocorrência/), { target: { value: "2030-10-20" } });
  fireEvent.change(screen.getByLabelText(/Valor/), { target: { value: amount } });
  fireEvent.change(screen.getByLabelText(/^Descrição/), { target: { value: detail } });
}
function addDebt(detail = "Detalhe informado", amount = "150,00") {
  fillDebt(detail, amount);
  fireEvent.click(screen.getByRole("button", { name: "Adicionar débito" }));
}
function review() {
  fireEvent.click(screen.getByRole("button", { name: "Revisar cobrança" }));
}
function submit() {
  fireEvent.click(screen.getByRole("button", { name: "Gerar cobrança" }));
}

describe("Operational direct charge form", { timeout: 15_000 }, () => {
  it("navigates to the persisted charge success URL after creation", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    submit();
    await waitFor(() =>
      expect(mocks.replace).toHaveBeenCalledWith(
        `/direct-charges/success?chargeId=${directChargeReceipt.id}`,
      ),
    );
  });

  it("loads confirmation from the URL charge ID without posting again", async () => {
    render(<DirectChargeSuccessClient chargeId={directChargeReceipt.id} />);
    await screen.findByText("Cobrança criada com sucesso.");
    expect(screen.getByText(directChargeReceipt.customerName!)).toBeInTheDocument();
    expect(mocks.refreshStatus).toHaveBeenCalledWith(directChargeReceipt.id, expect.anything());
    expect(mocks.submit).not.toHaveBeenCalled();
    expect(screen.getByRole("link", { name: "Nova Cobrança Direta" })).toHaveAttribute(
      "href",
      "/direct-charges/new",
    );
    mocks.refreshStatus.mockResolvedValueOnce({ ...directChargeReceipt, customerName: "Nome persistido atualizado" });
    fireEvent.click(screen.getByRole("button", { name: "Atualizar situação" }));
    await screen.findByText("Nome persistido atualizado");
    expect(mocks.submit).not.toHaveBeenCalled();
  });

  it.each([
    ["email", "locked@example.invalid", null],
    ["phone", null, "11999990000"],
    ["both", "locked@example.invalid", "11999990000"],
  ])(
    "locks only the contacts returned by the financial lookup: %s",
    async (_kind, email, phone) => {
      mocks.customers.mockResolvedValue({
        ...directCustomers,
        items: [{ ...directCustomers.items[0], email, phone, documentType: "CPF" }],
      });
      render(<DirectChargeClient />);
      await fillBasics();
      const emailField = screen.getByLabelText("E-mail");
      const phoneField = screen.getByLabelText("Telefone");
      expect(emailField).toHaveValue(email ?? "");
      expect(phoneField).toHaveValue(phone ?? "");
      expect(screen.getByLabelText("Nome")).toBeDisabled();
      expect(emailField.hasAttribute("disabled")).toBe(Boolean(email));
      expect(phoneField.hasAttribute("disabled")).toBe(Boolean(phone));
      if (email) {
        fireEvent.change(emailField, { target: { value: "changed@example.invalid" } });
        expect(emailField).toHaveValue(email);
      }
      fireEvent.change(screen.getByLabelText(/CPF\/CNPJ/), { target: { value: "98765432100" } });
      expect(emailField).toHaveValue("");
      expect(phoneField).toHaveValue("");
      expect(emailField).toBeDisabled();
      expect(phoneField).toBeDisabled();
    },
  );

  it("sends optional manually entered contacts with the complete customer data", async () => {
    render(<DirectChargeClient />);
    await fillBasics();
    fireEvent.change(screen.getByLabelText("E-mail"), {
      target: { value: "manual@example.invalid" },
    });
    fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999990000" } });
    fireEvent.click(screen.getByRole("button", { name: "Adicionar débitos" }));
    addDebt();
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    expect(mocks.submit.mock.calls[0][0]).toMatchObject({
      customerName: "Cliente retornado",
      customerDocumentType: "CPF",
      customerDocument: "12345678901",
      customerEmail: "manual@example.invalid",
      customerPhone: "11999990000",
    });
  });

  it("looks up a complete document on Tab with blocking loading and no duplicate button request", async () => {
    let complete!: (value: typeof directCustomers) => void;
    mocks.customers.mockImplementation(
      () =>
        new Promise((resolve) => {
          complete = resolve;
        }),
    );
    const user = userEvent.setup();
    render(<DirectChargeClient />);
    await ready();
    const field = screen.getByLabelText(/CPF\/CNPJ/);
    await user.click(field);
    fireEvent.change(field, { target: { value: "12345678901" } });
    await user.tab();
    await screen.findByRole("dialog", { name: "Consultando cliente..." });
    fireEvent.click(screen.getByRole("button", { name: "Consultar", hidden: true }));
    expect(mocks.customers).toHaveBeenCalledTimes(1);
    await act(async () => {
      complete(directCustomers);
    });
    await waitFor(() => expect(screen.getByLabelText("Nome")).toHaveValue("Cliente retornado"));
    fireEvent.blur(field);
    expect(mocks.customers).toHaveBeenCalledTimes(1);
  });

  it("does not automatically query an incomplete document", async () => {
    render(<DirectChargeClient />);
    await ready();
    const field = screen.getByLabelText(/CPF\/CNPJ/);
    fireEvent.change(field, { target: { value: "123" } });
    fireEvent.blur(field);
    expect(mocks.customers).not.toHaveBeenCalled();
  });

  it("concatenates the description in the step-two grid and resets the text for the next item", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    choose(/Categoria Yalla/, "Multa do banco");
    fireEvent.change(screen.getByLabelText(/Ocorrência/), { target: { value: "2030-10-20" } });
    fireEvent.change(screen.getByLabelText(/Valor/), { target: { value: "150,00" } });
    fireEvent.change(screen.getByLabelText(/^Descrição/), {
      target: { value: "Texto do operador" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Adicionar débito" }));
    expect(screen.getByRole("table")).toHaveTextContent(
      "MULTA DO BANCO | Texto do operador | CT-TEST",
    );
    for (const name of ["Categoria", "Descrição", "Ocorrência", "Valor", "Ações"])
      expect(screen.getByRole("columnheader", { name: new RegExp(name) })).toBeInTheDocument();
    expect(within(screen.getByRole("table")).getByText("Multa do banco")).toBeInTheDocument();
    expect(screen.getByLabelText(/^Descrição/)).toHaveValue("");
    expect(screen.getByRole("table")).not.toHaveTextContent("ABC1D23");
  });

  it("refreshes the simulated status from the backend without creating another charge", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    fireEvent.click(screen.getByRole("button", { name: "Atualizar situação" }));
    await waitFor(() =>
      expect(mocks.refreshStatus).toHaveBeenCalledWith(
        directChargeReceipt.id,
        expect.objectContaining({ contextId: "session-test", signal: expect.any(AbortSignal) }),
      ),
    );
    await waitFor(() =>
      expect(mocks.toast).toHaveBeenCalledWith({
        message: "Situação da cobrança atualizada.",
        severity: "success",
      }),
    );
    expect(mocks.submit).toHaveBeenCalledTimes(1);
  });

  it("blocks during status refresh and shows a recoverable failure only through toast", async () => {
    let reject!: (failure: Error) => void;
    mocks.refreshStatus.mockImplementationOnce(
      () =>
        new Promise((_resolve, fail) => {
          reject = fail;
        }),
    );
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    const button = screen.getByRole("button", { name: "Atualizar situação" });
    fireEvent.click(button);
    fireEvent.click(button);
    await screen.findByRole("dialog", { name: "Atualizando situação da cobrança..." });
    expect(mocks.refreshStatus).toHaveBeenCalledTimes(1);
    await act(async () => {
      reject(new Error("unavailable"));
    });
    await waitFor(() =>
      expect(mocks.toast).toHaveBeenCalledWith({
        message: "Não foi possível atualizar a situação da cobrança. Tente novamente.",
        severity: "error",
      }),
    );
    await ready();
    expect(screen.getByText("Cobrança criada com sucesso.")).toBeInTheDocument();
    expect(
      screen.queryByText("Não foi possível atualizar a situação da cobrança. Tente novamente."),
    ).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Atualizar situação" }));
    await waitFor(() => expect(mocks.refreshStatus).toHaveBeenCalledTimes(2));
  });

  it("preserves confirmation and its customer when retrying a catalog refresh", async () => {
    const view = render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    mocks.centers.mockRejectedValueOnce(new Error("catalog temporarily unavailable"));
    await act(async () => {
      await view.client.refetchQueries({ queryKey: ["financial-cost-centers"] });
    });
    const retry = await screen.findByRole("button", { name: "Tentar novamente" });
    fireEvent.click(retry);
    await waitFor(() =>
      expect(screen.queryByRole("button", { name: "Tentar novamente" })).not.toBeInTheDocument(),
    );
    await ready();
    expect(screen.getByText("Cobrança criada com sucesso.")).toBeInTheDocument();
    expect(screen.getByText("Cliente retornado")).toBeInTheDocument();
    expect(screen.getAllByText("Operação do catálogo").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Nova Cobrança Direta" })).toBeEnabled();
  });
  it.each([
    "O sistema financeiro recusou o acesso da integração. Verifique a credencial e as permissões da conexão.",
    "O sistema financeiro retornou dados inválidos ou uma paginação inconsistente. Tente novamente; se persistir, contate o suporte.",
    "Não foi possível acessar a API.",
  ])("shows the actual catalog failure and preserves retry: %s", async (message) => {
    mocks.centers.mockRejectedValueOnce(new ApiError("ServiceUnavailable", message, 503));
    render(<DirectChargeClient />);
    await expectError(`Não foi possível carregar os centros de custo. ${message}`);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
    await waitFor(() =>
      expect(screen.queryByRole("button", { name: "Tentar novamente" })).not.toBeInTheDocument(),
    );
  });
  it("starts empty, loads real catalogs and uses the returned customer name", async () => {
    render(<DirectChargeClient />);
    await ready();
    for (const label of [/CPF\/CNPJ/, /Contrato/, /Placa/, /Vencimento/, /Observação ao cliente/])
      expect(screen.getByLabelText(label)).toHaveValue("");
    expect(screen.queryByText(/Maria Silva|CT-1024|123\.456/)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeDisabled();
    await fillBasics();
    expect(mocks.customers).toHaveBeenCalledWith(
      "12345678901",
      expect.objectContaining({ contextId: "session-test", signal: expect.any(AbortSignal) }),
    );
    expect(screen.getByLabelText(/CPF\/CNPJ/)).toHaveValue("123.456.789-01");
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeEnabled();
    expect(mocks.centers).toHaveBeenCalled();
    expect(mocks.categories).toHaveBeenCalled();
  });

  it("requires all basics and invalidates the customer when the document changes", async () => {
    render(<DirectChargeClient />);
    await fillBasics();
    const next = screen.getByRole("button", { name: "Adicionar débitos" });
    fireEvent.change(screen.getByLabelText(/Placa/), { target: { value: "AB12345" } });
    expect(next).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/Placa/), { target: { value: "ABC1D23" } });
    expect(next).toBeEnabled();
    for (const [label, value] of [
      [/Contrato/, "CT-TEST"],
      [/Placa/, "ABC1D23"],
      [/Vencimento/, "2030-10-30"],
    ] as const) {
      fireEvent.change(screen.getByLabelText(label), { target: { value: "" } });
      expect(next).toBeDisabled();
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
      expect(next).toBeEnabled();
    }
    fireEvent.change(screen.getByLabelText(/CPF\/CNPJ/), { target: { value: "98765432100" } });
    expect(screen.queryByText("Cliente: Cliente retornado")).not.toBeInTheDocument();
    expect(next).toBeDisabled();
  });

  it.each(["not-found", "ambiguous", "error"])(
    "handles customer lookup failure without invented customers: %s",
    async (kind) => {
      if (kind === "error") mocks.customers.mockRejectedValue(new Error("provider failure"));
      else
        mocks.customers.mockResolvedValue({
          ...directCustomers,
          totalCount: kind === "not-found" ? 0 : 2,
          items:
            kind === "not-found"
              ? []
              : [
                  ...directCustomers.items,
                  { ...directCustomers.items[0], externalReference: "other" },
                ],
        });
      render(<DirectChargeClient />);
      await ready();
      fireEvent.change(screen.getByLabelText(/CPF\/CNPJ/), { target: { value: "12345678901" } });
      fireEvent.click(screen.getByRole("button", { name: "Consultar" }));
      await expectError(
        kind === "not-found"
          ? "Cliente não encontrado no sistema financeiro."
          : kind === "ambiguous"
            ? /Mais de um cliente encontrado/
            : /Não foi possível consultar o cliente/,
      );
      await ready();
      expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeDisabled();
      expect(screen.queryByText("Cliente: Cliente retornado")).not.toBeInTheDocument();
    },
  );

  it("recovers failed catalog loading through retry without falling back to mock options", async () => {
    mocks.centers.mockRejectedValueOnce(new Error("unavailable"));
    render(<DirectChargeClient />);
    await expectError("Não foi possível carregar os centros de custo.");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
    await waitFor(() =>
      expect(screen.queryByRole("button", { name: "Tentar novamente" })).not.toBeInTheDocument(),
    );
    await fillBasics();
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeEnabled();
  });

  it("blocks advancing if the lookup and cost centers came from different financial connections", async () => {
    mocks.customers.mockResolvedValue({ ...directCustomers, connectionId: "changed-connection" });
    render(<DirectChargeClient />);
    await ready();
    fireEvent.change(screen.getByLabelText(/CPF\/CNPJ/), { target: { value: "12345678901" } });
    fireEvent.click(screen.getByRole("button", { name: "Consultar" }));
    await expectError(/A conexão financeira mudou/);
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeDisabled();
  });

  it("starts with no debts, validates pending input and excludes inactive categories", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    expect(screen.getByText("Nenhum débito adicionado.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Adicionar débito" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeDisabled();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /Categoria Yalla/ }));
    expect(screen.queryByRole("option", { name: "Categoria inativa" })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("option", { name: "Multa do banco" }));
    expect(screen.getByRole("button", { name: "Adicionar débito" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
    addDebt();
    expect(screen.getByLabelText(/^Descrição/)).toHaveValue("");
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeEnabled();
    fireEvent.change(screen.getByLabelText(/^Descrição/), {
      target: { value: "Incomplete" },
    });
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeEnabled();
  });

  it("edits and removes actual draft items without duplicating, and keeps the real summary", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    fireEvent.click(screen.getByRole("button", { name: "Editar débito 1" }));
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/^Descrição/), {
      target: { value: "Atualizado" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Salvar alteração" }));
    review();
    expect(screen.getByText("MULTA DO BANCO | Atualizado | CT-TEST")).toBeInTheDocument();
    const panel = within(
      screen.getByRole("heading", { name: "Dados da cobrança" }).closest("section")!,
    );
    expect(panel.getByText("Cliente retornado")).toBeInTheDocument();
    expect(panel.getByText("Operação do catálogo")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Voltar aos débitos" }));
    fireEvent.click(screen.getByRole("button", { name: "Remover débito 1" }));
    expect(screen.getByRole("button", { name: "Revisar cobrança" })).toBeDisabled();
  });

  it("posts all pages and advances to the shared confirmation with the backend simulation identifier", async () => {
    render(<DirectChargeClient />);
    await openDebts();
    for (let index = 0; index < 5; index++)
      addDebt(`Item ${index + 1}`, "1.801.439.850.948.198,01");
    const table = screen.getByRole("table", { name: "Débitos da Cobrança Direta" });
    expect(within(table).getAllByRole("row")).toHaveLength(4);
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    expect(screen.getByText(directChargeReceipt.reference)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Gerar cobrança" })).not.toBeInTheDocument();
    expect(screen.getByText("Integração agendada.")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "Abrir débito" })).toHaveLength(5);
    expect(screen.getByRole("list", { name: "Lançamentos por item" }).children).toHaveLength(5);
    expect(screen.getAllByText("R$ 1.801.439.850.948.198,01")).toHaveLength(5);
    expect(screen.getByText("R$ 9.007.199.254.740.990,05")).toBeInTheDocument();
    const body = mocks.submit.mock.calls[0][0];
    expect(body).toMatchObject({
      customerDocument: "12345678901",
      costCenterReference: "center-a",
      costCenterName: "Operação do catálogo",
    });
    expect(body.items).toHaveLength(5);
    expect(body.items[0]).toMatchObject({
      categoryId: directCategoryId,
      amount: "1801439850948198.01",
    });
    expect(screen.queryByRole("heading", { name: "Pagamento" })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Nova Cobrança Direta" }));
    expect(screen.getByLabelText(/Contrato/)).toHaveValue("");
    expect(screen.getByLabelText(/CPF\/CNPJ/)).toHaveValue("");
  });

  it("blocks the viewport and prevents duplicate submit clicks", async () => {
    let complete!: (receipt: typeof directChargeReceipt) => void;
    mocks.submit.mockImplementation(
      () =>
        new Promise((resolve) => {
          complete = resolve;
        }),
    );
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    const button = screen.getByRole("button", { name: "Gerar cobrança" });
    fireEvent.click(button);
    fireEvent.click(button);
    await screen.findByRole("dialog", { name: "Enviando solicitação..." });
    expect(mocks.submit).toHaveBeenCalledTimes(1);
    await act(async () => {
      complete(directChargeReceipt);
    });
    await screen.findByText("Cobrança criada com sucesso.");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("preserves the draft and attempt after failure, and uses a new key for changed content", async () => {
    mocks.submit
      .mockRejectedValueOnce(new Error("unavailable"))
      .mockRejectedValueOnce(new Error("unavailable"));
    render(<DirectChargeClient />);
    await openDebts();
    addDebt();
    review();
    submit();
    await expectError("Não foi possível enviar a solicitação. Tente novamente.");
    const key = mocks.submit.mock.calls[0][1];
    fireEvent.click(screen.getByRole("button", { name: "Voltar aos débitos" }));
    fireEvent.click(screen.getByRole("button", { name: "Editar débito 1" }));
    fireEvent.click(screen.getByRole("button", { name: "Salvar alteração" }));
    review();
    mocks.toast.mockClear();
    submit();
    await expectError("Não foi possível enviar a solicitação. Tente novamente.");
    expect(mocks.submit.mock.calls[1][1]).toBe(key);
    fireEvent.click(screen.getByRole("button", { name: "Voltar aos débitos" }));
    fireEvent.click(screen.getByRole("button", { name: "Editar débito 1" }));
    fireEvent.change(screen.getByLabelText(/^Descrição/), {
      target: { value: "Outro detalhe" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Salvar alteração" }));
    review();
    submit();
    await screen.findByText("Cobrança criada com sucesso.");
    expect(mocks.submit.mock.calls[2][1]).not.toBe(key);
  });

  it("clears data on context change and ignores a previous context's pending submission", async () => {
    let complete!: (receipt: typeof directChargeReceipt) => void;
    mocks.submit.mockImplementation(
      () =>
        new Promise((resolve) => {
          complete = resolve;
        }),
    );
    const client = createQueryClient();
    function Page({ contextId }: { contextId: string }) {
      return (
        <QueryClientProvider client={client}>
          <SessionProvider session={{ tenantId: contextId, contextId }}>
            <DirectChargeClient />
          </SessionProvider>
        </QueryClientProvider>
      );
    }
    const view = renderRaw(<Page contextId="context-a" />);
    await openDebts();
    addDebt();
    review();
    submit();
    await screen.findByRole("dialog", { name: "Enviando solicitação..." });
    view.rerender(<Page contextId="context-b" />);
    await ready();
    expect(screen.getByLabelText(/Contrato/)).toHaveValue("");
    expect(mocks.submit.mock.calls[0][2].signal.aborted).toBe(true);
    await act(async () => {
      complete(directChargeReceipt);
    });
    expect(screen.queryByText("Cobrança criada com sucesso.")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Adicionar débitos" })).toBeDisabled();
  });
});
