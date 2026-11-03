import { afterEach, describe, expect, it, vi, beforeEach } from "vitest";
import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { FinanceSessionSummary } from "./FinanceSessionSummary";
import { renderWithSession } from "@/test/renderWithSession";
import { directoryApi } from "@/features/users/directory.client";
const { replace, refresh } = vi.hoisted(() => ({ replace: vi.fn(), refresh: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace, refresh }) }));
vi.mock("@/features/auth/actions", () => ({ logoutAction: vi.fn() }));
vi.mock("@/features/users/directory.client", async (original) => ({
  ...await original<typeof import("@/features/users/directory.client")>(),
  directoryApi: { get: vi.fn(), send: vi.fn() },
}));

beforeEach(() => { vi.clearAllMocks(); });
afterEach(cleanup);

describe("FinanceSessionSummary", () => {
  it("omits optional fields when the session does not provide them", () => {
    renderWithSession(<FinanceSessionSummary session={{ canLogout: true, contextId: "session-test", displayName: "Usuário de teste" }} />);

    expect(screen.getByText("Usuário de teste")).toBeInTheDocument();
    expect(screen.getByText("UD")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sair" })).toBeInTheDocument();
    expect(screen.queryByText("Administrador")).not.toBeInTheDocument();
  });

  it("places session actions in a separate row after the user's identity", () => {
    renderWithSession(<FinanceSessionSummary session={{ canLogout: true, contextId: "session-test", displayName: "Usuário de teste" }} />);

    const identity = screen.getByRole("group", { name: "Dados do usuário" });
    const actions = screen.getByRole("group", { name: "Ações da sessão" });

    expect(identity.parentElement).toBe(actions.parentElement);
    expect(identity.nextElementSibling).toBe(actions);
    expect(identity.parentElement).toHaveStyle({ display: "grid" });
  });

  it("keeps the organization visually grouped with the user's identity", () => {
    renderWithSession(<FinanceSessionSummary session={{ canLogout: true, displayName: "Usuário de teste", organization: "Yalla Finance" }} />);

    const identity = screen.getByRole("group", { name: "Dados do usuário" });
    const organization = screen.getByText("Yalla Finance");

    expect(identity).toContainElement(organization);
  });

  it("does not render an empty session block", () => {
    const { container } = renderWithSession(<FinanceSessionSummary session={{ canLogout: false }} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("opens the current user's password modal from the extensible avatar menu", async () => {
    vi.mocked(directoryApi.send).mockResolvedValue(undefined);
    renderWithSession(<FinanceSessionSummary session={{ canLogout: true, contextId: "session-test", displayName: "Usuário de teste" }} />);

    fireEvent.click(screen.getByRole("button", { name: "Configurações da sessão" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: "Alterar senha" }));
    const dialog = await screen.findByRole("dialog", { name: "Alterar minha senha" });
    fireEvent.change(screen.getByLabelText("Senha atual"), { target: { value: "senha-atual" } });
    fireEvent.change(screen.getByLabelText("Nova senha"), { target: { value: "senha-nova-segura-123" } });
    fireEvent.change(screen.getByLabelText("Confirmar nova senha"), { target: { value: "senha-nova-segura-123" } });
    fireEvent.click(screen.getByRole("button", { name: "Alterar senha e sair" }));

    await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/auth/password", "POST",
      { currentPassword: "senha-atual", newPassword: "senha-nova-segura-123" }, "session-test"));
    expect(replace).toHaveBeenCalledWith("/login");
    expect(refresh).toHaveBeenCalled();
    expect(dialog).toBeInTheDocument();
  });
});
