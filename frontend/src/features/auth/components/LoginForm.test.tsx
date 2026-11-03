import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { LoginForm } from "./LoginForm";
import { type LoginActionState } from "../authSession";

const mocks = vi.hoisted(() => ({ searchParams: new URLSearchParams() }));
vi.mock("@/features/auth/actions", () => ({ loginAction: vi.fn() }));
vi.mock("next/navigation", () => ({ useSearchParams: () => mocks.searchParams }));

beforeEach(() => { mocks.searchParams = new URLSearchParams(); });
afterEach(() => cleanup());

it("preserves the submitted payload and existing defaults when selecting a company", async () => {
  const action = vi.fn<(state: LoginActionState, data: FormData) => Promise<LoginActionState>>(
    async () => ({
      fieldErrors: {},
      email: "usuario",
      formError: "Selecione a empresa para continuar.",
      tenantOptions: [
        { id: "a", name: "Empresa A" },
        { id: "b", name: "Empresa B" },
      ],
    }),
  );
  render(<LoginForm action={action} />);
  expect(screen.getByLabelText("Login")).toHaveValue("");
  expect(screen.getByLabelText("Senha")).toHaveValue("");
  fireEvent.change(screen.getByLabelText("Login"), { target: { value: "usuario" } });
  fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "senha-teste" } });
  fireEvent.click(screen.getByRole("button", { name: "Entrar" }));
  const select = await screen.findByRole("combobox", { name: "Empresa" });
  const user = userEvent.setup();
  await user.click(select);
  await user.click(await screen.findByRole("option", { name: "Empresa B" }));
  await user.click(screen.getByRole("button", { name: "Entrar" }));
  await waitFor(() => expect(action).toHaveBeenCalledTimes(2));
  expect(action.mock.calls[1][1].get("email")).toBe("usuario");
  expect(action.mock.calls[0][1].get("password")).toBe("senha-teste");
  expect(action.mock.calls[1][1].get("password")).toBe("senha-teste");
  expect(action.mock.calls[1][1].get("tenantId")).toBe("b");
});

it("explains that the API is unavailable instead of claiming the session expired", () => {
  mocks.searchParams = new URLSearchParams("error=api-unavailable");
  render(<LoginForm action={async () => ({ fieldErrors: {}, formError: "" })} />);
  expect(screen.getByText("A conexão com o sistema foi perdida. Verifique sua conexão e tente entrar novamente.")).toBeInTheDocument();
  mocks.searchParams = new URLSearchParams();
});
