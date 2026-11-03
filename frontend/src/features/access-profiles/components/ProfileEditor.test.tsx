import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { ProfileEditor } from "./ProfileEditor";
import type { FunctionalityOption } from "@/features/users/types";

const functionality: FunctionalityOption[] = [
  { id: "home-id", code: "HOME", name: "Início", description: "Acessar o painel inicial." },
  { id: "debt-id", code: "DEBTS", name: "Débitos", description: "Consultar débitos." },
];
afterEach(cleanup);

it("starts a new profile without grants and saves the selected functionality IDs", () => {
  const onSave = vi.fn();
  render(<ProfileEditor profile={null} functionalities={functionality} busy={false} onClose={vi.fn()} onSave={onSave} />);
  expect(screen.getByRole("checkbox", { name: "Início" })).not.toBeChecked();
  fireEvent.change(screen.getByLabelText("Código"), { target: { value: "OPERADOR" } });
  fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "Operador" } });
  fireEvent.click(screen.getByRole("checkbox", { name: "Débitos" }));
  fireEvent.click(screen.getByRole("button", { name: "Salvar" }));
  expect(onSave).toHaveBeenCalledWith({ code: "OPERADOR", name: "Operador", active: true, functionalityIds: ["debt-id"] });
});

it("loads and allows editing only persisted grants for ADMIN", () => {
  const onSave = vi.fn();
  render(<ProfileEditor profile={{ id: "admin", code: "admin", name: "Administrador", active: true, functionalityIds: ["home-id"] }}
    functionalities={functionality} busy={false} onClose={vi.fn()} onSave={onSave} />);
  const form = screen.getByRole("heading", { name: "Controle de acesso" }).closest("form")!;
  expect(within(form).getByRole("checkbox", { name: "Início" })).toBeChecked();
  const debts = within(form).getByRole("checkbox", { name: "Débitos" });
  expect(debts).not.toBeChecked();
  expect(debts).toBeEnabled();
  fireEvent.click(debts);
  fireEvent.click(within(form).getByRole("button", { name: "Salvar" }));
  expect(onSave).toHaveBeenCalledWith({ code: "admin", name: "Administrador", active: true, functionalityIds: ["home-id", "debt-id"] });
});
