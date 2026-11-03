import { cleanup } from "@testing-library/react";
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { directoryApi } from "../directory.client";
import { UsersPage } from "./UsersPage";
afterEach(() => { cleanup(); vi.resetAllMocks(); });
vi.mock("../directory.client", async (importOriginal) => ({ ...await importOriginal<typeof import("@/features/users/directory.client")>(), directoryApi: { get: vi.fn(), send: vi.fn() } }));
it("navigates to the shared registration page without password actions in the listing", async () => {
  vi.mocked(directoryApi.get).mockImplementation(async (path) => {
    if (path.startsWith("/api/access-profiles")) return { items: [], totalCount: 0, page: 1, pageSize: 20 };
    if (path === "/api/users/initial-password") return { password: "senha inicial da empresa" };
    return { items: [], totalCount: 0, page: 1, pageSize: 20 };
  });
  renderWithSession(<UsersPage />);
  expect(await screen.findByText("Nenhum usuário encontrado.")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Senha inicial da empresa" })).not.toBeInTheDocument();
  expect(await screen.findByRole("link", { name: "Novo usuário" })).toHaveAttribute("href", "/users/new");
  expect(screen.queryByRole("link", { name: "Alterar minha senha" })).not.toBeInTheDocument();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Senha inicial")).not.toBeInTheDocument();
});
it("unblocks after failure and offers retry", async () => {
  vi.mocked(directoryApi.get).mockRejectedValue(new Error("Falha simulada"));
  renderWithSession(<UsersPage />);
  expect(await screen.findByText("Não foi possível consultar os usuários.")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Tentar novamente" })).toBeEnabled();
});
it("applies and clears user filters using the standard filter actions", async () => {
  vi.mocked(directoryApi.get).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  renderWithSession(<UsersPage />);
  await screen.findByText("Nenhum usuário encontrado.");
  fireEvent.change(screen.getByLabelText("Nome, login ou e-mail"), { target: { value: "Maria" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  await waitFor(() => expect(vi.mocked(directoryApi.get).mock.calls.some(([path]) => path.includes("search=Maria"))).toBe(true));
  await waitFor(() => expect(screen.getByRole("button", { name: "Limpar" })).toBeEnabled());
  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  expect(screen.getByLabelText("Nome, login ou e-mail")).toHaveValue("");
  await waitFor(() => expect(vi.mocked(directoryApi.get).mock.calls.at(-1)?.[0]).not.toContain("search=Maria"));
});
it("offers only navigation to editing in the user row", async () => {
  const user = { id: "user-a", login: "maria", name: "Maria", email: "maria@example.test",
    active: true, accessActive: true, profileId: null, profileName: null };
  vi.mocked(directoryApi.get).mockImplementation(async (path) => path.startsWith("/api/access-profiles")
    ? { items: [], totalCount: 0, page: 1, pageSize: 20 }
    : { items: [user], totalCount: 1, page: 1, pageSize: 20 });
  renderWithSession(<UsersPage />);
  const table = await screen.findByRole("table", { name: "Usuários da empresa" });
  await within(table).findByText("Maria");
  expect(within(table).queryByRole("button", { name: "Redefinir senha" })).not.toBeInTheDocument();
  for (const name of ["Desativar usuário", "Desativar vínculo", "Vincular empresa"])
    expect(within(table).queryByRole("button", { name })).not.toBeInTheDocument();
  expect(within(table).getByRole("link", { name: "Editar" })).toHaveAttribute("href", "/users/user-a");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
});
