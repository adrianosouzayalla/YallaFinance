import { cleanup, fireEvent, waitFor } from "@testing-library/react";
import { screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { directoryApi } from "@/features/users/directory.client";
import { ProfilesPage } from "./ProfilesPage";
afterEach(() => { cleanup(); vi.resetAllMocks(); });
vi.mock("@/features/users/directory.client", async (importOriginal) => ({ ...await importOriginal<typeof import("@/features/users/directory.client")>(), directoryApi: { get: vi.fn(), send: vi.fn() } }));
it("lists profiles as registrations without permission controls", async () => {
  vi.mocked(directoryApi.get).mockResolvedValue({ items: [{ id: "a", code: "OPERADOR", name: "Operador", active: true }], totalCount: 1, page: 1, pageSize: 20 });
  renderWithSession(<ProfilesPage />);
  expect(await screen.findByText("OPERADOR")).toBeInTheDocument();
  await waitFor(() => expect(screen.getByRole("link", { name: "Novo perfil" })).toHaveAttribute("href", "/profiles/new"));
  expect(screen.getByRole("link", { name: "Editar" })).toHaveAttribute("href", "/profiles/a");
  expect(screen.queryByRole("button", { name: "Desativar" })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /permissões/i })).not.toBeInTheDocument();
});
it("applies and clears profile filters using the standard filter actions", async () => {
  vi.mocked(directoryApi.get).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  renderWithSession(<ProfilesPage />);
  await screen.findByText("Nenhum perfil encontrado.");
  fireEvent.change(screen.getByLabelText("Nome ou código"), { target: { value: "OPERADOR" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  await waitFor(() => expect(vi.mocked(directoryApi.get).mock.calls.some(([path]) => path.includes("search=OPERADOR"))).toBe(true));
  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  expect(screen.getByLabelText("Nome ou código")).toHaveValue("");
  await waitFor(() => expect(vi.mocked(directoryApi.get).mock.calls.at(-1)?.[0]).not.toContain("search=OPERADOR"));
});
