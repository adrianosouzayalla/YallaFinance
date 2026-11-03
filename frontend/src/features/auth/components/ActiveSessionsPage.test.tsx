import { fireEvent, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { sessionsApi } from "../api/sessions.client";
import { ActiveSessionsPage } from "./ActiveSessionsPage";

vi.mock("../api/sessions.client", () => ({ sessionsApi: { list: vi.fn() } }));

it("lists sessions without requiring a profile and offers refresh without termination controls", async () => {
  vi.mocked(sessionsApi.list).mockResolvedValue({
    items: [{ rowKey: "row-a", displayName: "Administrador", email: "admin@yalla.com.br", createdAt: "2026-10-17T12:00:00Z",
      expiresAt: "2026-10-17T20:00:00Z", isCurrent: true }], totalCount: 1, page: 1, pageSize: 20,
  });
  renderWithSession(<ActiveSessionsPage />);
  expect(await screen.findByText("admin@yalla.com.br")).toBeInTheDocument();
  expect(screen.getByText("Sessão atual")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /encerrar|revogar/i })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Atualizar" }));
  await waitFor(() => expect(sessionsApi.list).toHaveBeenCalledTimes(2));
});

it("shows recoverable errors and a normal empty state after retry", async () => {
  vi.mocked(sessionsApi.list).mockRejectedValueOnce(new Error("Unavailable"))
    .mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  renderWithSession(<ActiveSessionsPage />);
  expect(await screen.findByText(/Não foi possível consultar/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
  expect(await screen.findByText("Nenhuma sessão ativa encontrada.")).toBeInTheDocument();
});
