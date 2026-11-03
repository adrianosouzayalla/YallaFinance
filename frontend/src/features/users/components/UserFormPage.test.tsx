import { cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { directoryApi } from "../directory.client";
import { UserFormPage } from "./UserFormPage";
const { push, replace, refresh } = vi.hoisted(() => ({ push: vi.fn(), replace: vi.fn(), refresh: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push, replace, refresh }) }));
vi.mock("../directory.client", async (original) => ({ ...await original<typeof import("../directory.client")>(), directoryApi: { get: vi.fn(), send: vi.fn() } }));
afterEach(() => { cleanup(); vi.resetAllMocks(); });
const user = { id: "user-a", login: "maria", name: "Maria", email: "maria@example.test", active: true,
  accessActive: true, profileId: null, profileName: null };
function mockReads() {
  vi.mocked(directoryApi.get).mockImplementation(async (path) => {
    if (path.startsWith("/api/access-profiles")) return { items: [], totalCount: 0, page: 1, pageSize: 20 };
    if (path === "/api/users/initial-password") return { password: "senha inicial da empresa" };
    return user;
  });
}
it("creates a user using the shared page form and opens editing after success", async () => {
  mockReads(); vi.mocked(directoryApi.send).mockResolvedValue(user);
  renderWithSession(<UserFormPage />);
  expect(await screen.findByText("A senha inicial será: senha inicial da empresa")).toBeInTheDocument();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Login"), { target: { value: "maria" } });
  fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "Maria" } });
  fireEvent.change(screen.getByLabelText("E-mail na empresa"), { target: { value: "maria@example.test" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));
  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/users", "POST",
    { login: "maria", name: "Maria", email: "maria@example.test", active: true, profileId: null }, "session-test"));
  await waitFor(() => expect(push).toHaveBeenCalledWith("/users/user-a"));
});
it("edits on a page and preserves values after a failed save", async () => {
  mockReads(); vi.mocked(directoryApi.send).mockRejectedValue(new Error("Falha simulada"));
  renderWithSession(<UserFormPage userId="user-a" />);
  await screen.findByDisplayValue("Maria");
  expect(screen.getByLabelText("Login")).toBeDisabled();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /ativar vínculo/i })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Vincular empresa" })).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "Maria Silva" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));
  expect(await screen.findByText("Falha simulada")).toBeInTheDocument();
  expect(screen.getByLabelText("Nome")).toHaveValue("Maria Silva");
  expect(directoryApi.send).toHaveBeenCalledWith("/api/users/user-a", "PUT",
    { name: "Maria Silva", email: "maria@example.test", profileId: null }, "session-test");
});
it("resets another user's password to the configured default after confirmation", async () => {
  mockReads(); vi.mocked(directoryApi.send).mockResolvedValue(undefined);
  renderWithSession(<UserFormPage userId="user-a" />, { userId: "user-z" });
  await screen.findByDisplayValue("Maria");
  fireEvent.click(screen.getByRole("button", { name: "Redefinir senha" }));
  expect(screen.queryByRole("button", { name: "Alterar minha senha" })).not.toBeInTheDocument();
  const reset = await screen.findByRole("dialog", { name: "Redefinir senha de Maria" });
  fireEvent.click(within(reset).getByRole("button", { name: "Confirmar redefinição" }));
  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/users/user-a/password", "POST",
    { confirmGlobalImpact: true }, "session-test"));
  expect(within(reset).queryByLabelText("Nova senha")).not.toBeInTheDocument();
});
it("keeps own password change in the session avatar menu instead of the user editor", async () => {
  mockReads();
  renderWithSession(<UserFormPage userId="user-a" />, { userId: "user-a" });
  await screen.findByDisplayValue("Maria");
  expect(screen.queryByRole("button", { name: "Alterar minha senha" })).not.toBeInTheDocument();
});
it("offers retry when the form cannot load", async () => {
  vi.mocked(directoryApi.get).mockRejectedValue(new Error("Falha simulada"));
  renderWithSession(<UserFormPage userId="user-a" />);
  expect(await screen.findByText("Não foi possível carregar o cadastro.")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Tentar novamente" })).toBeEnabled();
  expect(screen.queryByRole("button", { name: "Salvar" })).not.toBeInTheDocument();
});
