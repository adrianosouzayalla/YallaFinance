import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { SessionGuard } from "./SessionGuard";

const mocks = vi.hoisted(() => ({
  contextId: "context-a",
  session: vi.fn(),
  discardLocalSession: vi.fn(),
  queryClient: { cancelQueries: vi.fn(), clear: vi.fn() },
}));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ contextId: mocks.contextId }),
}));
vi.mock("@tanstack/react-query", () => ({ useQueryClient: () => mocks.queryClient }));
vi.mock("@/features/auth/api/auth.client", () => ({ authClientApi: { session: mocks.session } }));
vi.mock("@/features/auth/actions", () => ({ discardLocalSessionAction: mocks.discardLocalSession }));

beforeEach(() => {
  mocks.discardLocalSession.mockResolvedValue(undefined);
});

afterEach(() => {
  cleanup();
  mocks.contextId = "context-a";
  vi.clearAllMocks();
});

it("clears protected state and discards the local session when the API is unavailable", async () => {
  let finish!: () => void;
  mocks.session.mockImplementation(() => new Promise<void>((resolve) => { finish = resolve; }));
  const replace = vi.fn();
  const originalLocation = window.location;
  Object.defineProperty(window, "location", { configurable: true, value: { ...originalLocation, replace } });
  try {
    render(<SessionGuard><span>Conteúdo protegido</span></SessionGuard>);
    await act(async () => { finish(); });
    await screen.findByText("Conteúdo protegido");

    await act(async () => {
      window.dispatchEvent(new Event("yalla:api-unavailable"));
    });

    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login?error=api-unavailable"));
    expect(mocks.queryClient.cancelQueries).toHaveBeenCalledOnce();
    expect(mocks.queryClient.clear).toHaveBeenCalledOnce();
    expect(mocks.discardLocalSession).toHaveBeenCalledOnce();
  } finally {
    Object.defineProperty(window, "location", { configurable: true, value: originalLocation });
  }
});

it("validates each context before revealing its protected content and aborts the previous check", async () => {
  let finish!: () => void;
  mocks.session.mockImplementation(() => new Promise<void>((resolve) => { finish = resolve; }));
  const view = render(<SessionGuard><span>Conteúdo protegido</span></SessionGuard>);
  expect(screen.getByRole("dialog", { name: "Validando sessão..." })).toBeVisible();
  const firstSignal = mocks.session.mock.calls[0][0].signal as AbortSignal;
  await act(async () => { finish(); });
  expect(await screen.findByText("Conteúdo protegido")).toBeVisible();
  mocks.contextId = "context-b";
  view.rerender(<SessionGuard><span>Conteúdo protegido</span></SessionGuard>);
  expect(firstSignal.aborted).toBe(true);
  expect(screen.queryByText("Conteúdo protegido")).not.toBeInTheDocument();
  expect(screen.getByRole("dialog", { name: "Validando sessão..." })).toBeVisible();
  expect(mocks.session).toHaveBeenLastCalledWith({ contextId: "context-b", signal: expect.any(AbortSignal) });
  await act(async () => { finish(); });
  await waitFor(() => expect(screen.getByText("Conteúdo protegido")).toBeVisible());
});
