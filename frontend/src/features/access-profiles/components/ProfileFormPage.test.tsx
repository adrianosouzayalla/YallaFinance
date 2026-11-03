import { cleanup, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { directoryApi } from "@/features/users/directory.client";
import { ProfileFormPage } from "./ProfileFormPage";

const { push, showToast } = vi.hoisted(() => ({ push: vi.fn(), showToast: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ push }) }));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast }) }));
vi.mock("@/features/users/directory.client", async (original) => ({
  ...await original<typeof import("@/features/users/directory.client")>(),
  directoryApi: { get: vi.fn(), send: vi.fn() },
}));
afterEach(() => { cleanup(); vi.resetAllMocks(); });
beforeEach(() => { vi.mocked(directoryApi.get).mockResolvedValue(functionalities); });

const profile = { id: "profile-a", code: "OPERADOR", name: "Operador", active: true, functionalityIds: ["debt-id"] };
const functionalities = [{ id: "debt-id", code: "DEBTS", name: "Débitos", description: "Consultar débitos." }];

it("creates a profile on its own page and opens editing after saving", async () => {
  vi.mocked(directoryApi.get).mockResolvedValue(functionalities);
  vi.mocked(directoryApi.send).mockResolvedValue(profile);
  renderWithSession(<ProfileFormPage />);

  expect(await screen.findByRole("heading", { name: "Novo perfil" })).toBeInTheDocument();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Código"), { target: { value: "OPERADOR" } });
  fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "Operador" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));

  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/access-profiles", "POST",
    { code: "OPERADOR", name: "Operador", active: true, functionalityIds: [] }, "session-test"));
  await waitFor(() => expect(showToast).toHaveBeenCalledWith({ message: "Perfil salvo.", severity: "success" }));
  expect(push).toHaveBeenCalledWith("/profiles");
});

it("returns to the profile list and shows a toast after updating", async () => {
  vi.mocked(directoryApi.get).mockImplementation(async (path) => path.endsWith("/functionalities") ? functionalities : profile);
  vi.mocked(directoryApi.send).mockResolvedValue(undefined);
  renderWithSession(<ProfileFormPage profileId="profile-a" />);
  await screen.findByDisplayValue("Operador");
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));

  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/access-profiles/profile-a", "PUT",
    { name: "Operador", functionalityIds: ["debt-id"] }, "session-test"));
  await waitFor(() => expect(showToast).toHaveBeenCalledWith({ message: "Perfil salvo.", severity: "success" }));
  expect(push).toHaveBeenCalledWith("/profiles");
});

it("shows save failures in a toast", async () => {
  vi.mocked(directoryApi.send).mockRejectedValue(new Error("Falha ao salvar."));
  renderWithSession(<ProfileFormPage />);
  await screen.findByRole("heading", { name: "Controle de acesso" });
  fireEvent.change(screen.getByLabelText("Código"), { target: { value: "OPERADOR" } });
  fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "Operador" } });
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));

  await waitFor(() => expect(showToast).toHaveBeenCalledWith({ message: "Falha ao salvar.", severity: "error" }));
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  expect(push).not.toHaveBeenCalled();
});

it("edits a profile on its own page and changes status with confirmation", async () => {
  vi.mocked(directoryApi.get).mockImplementation(async (path) => path.endsWith("/functionalities") ? functionalities : profile);
  vi.mocked(directoryApi.send).mockResolvedValue(undefined);
  renderWithSession(<ProfileFormPage profileId="profile-a" />);

  expect(await screen.findByDisplayValue("Operador")).toBeInTheDocument();
  expect(screen.getByRole("checkbox", { name: "Débitos" })).toBeChecked();
  expect(screen.getByLabelText("Código")).toBeDisabled();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Desativar perfil" }));
  const confirmation = await screen.findByRole("dialog", { name: "Desativar perfil" });
  fireEvent.click(within(confirmation).getByRole("button", { name: "Confirmar" }));

  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/access-profiles/profile-a/active", "PUT",
    { active: false }, "session-test"));
});

it("saves the complete changed functionality selection when editing", async () => {
  vi.mocked(directoryApi.get).mockImplementation(async (path) => path.endsWith("/functionalities") ? functionalities : profile);
  vi.mocked(directoryApi.send).mockResolvedValue(undefined);
  renderWithSession(<ProfileFormPage profileId="profile-a" />);
  await screen.findByRole("heading", { name: "Controle de acesso" });
  fireEvent.click(screen.getByRole("checkbox", { name: "Débitos" }));
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));
  await waitFor(() => expect(directoryApi.send).toHaveBeenCalledWith("/api/access-profiles/profile-a", "PUT",
    { name: "Operador", functionalityIds: [] }, "session-test"));
});

it("returns to the profile list when canceling the page form", async () => {
  renderWithSession(<ProfileFormPage />);
  await screen.findByRole("heading", { name: "Novo perfil" });
  await screen.findByRole("heading", { name: "Controle de acesso" });
  fireEvent.click(screen.getByRole("button", { name: "Cancelar" }));
  expect(push).toHaveBeenCalledWith("/profiles");
});
