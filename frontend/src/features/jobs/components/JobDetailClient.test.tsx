import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ApiError } from "@/services/api/api-error";
import { JobDetailClient } from "./JobDetailClient";
const mock = vi.hoisted(() => ({
  detail: vi.fn(),
  executions: vi.fn(),
  mutation: vi.fn(),
  submit: vi.fn(),
}));
vi.mock("../hooks/useJob", () => ({ useJob: mock.detail }));
vi.mock("../hooks/useJobExecutions", () => ({ useJobExecutions: mock.executions }));
vi.mock("../hooks/useReprocessJob", () => ({ useReprocessJob: mock.mutation }));
beforeEach(() => {
  mock.submit.mockReset().mockResolvedValue({ message: "Job enviado para reprocessamento." });
  mock.mutation.mockReturnValue({ isPending: false, mutateAsync: mock.submit });
});
afterEach(cleanup);
const query = { isPending: false, isFetching: false, isError: false, refetch: vi.fn() };
it("shows only job execution information even when legacy data includes charge links", () => {
  mock.detail.mockReturnValue({
    ...query,
    data: {
      id: "job",
      type: "TYPE",
      jobTitle: "Criar cobrança no sistema financeiro externo",
      key: "charge:123",
      status: "Failed",
      chargeId: "123",
      payload: { jobParameter: "parameter-value" },
      createdAt: "2026-10-05T12:00:00Z",
      updatedAt: "2026-10-05T12:00:30Z",
    },
  });
  mock.executions.mockReturnValue({
    ...query,
    data: {
      items: [
        {
          id: "execution",
          startedAt: "2026-10-05T12:00:00Z",
          finishedAt: "2026-10-05T12:00:30Z",
          durationSeconds: 30,
          status: "Failed",
          errorCode: "JobHandlerError",
          result: { processed: 1 },
          details: { diagnostic: "diagnostic-value" },
          chargeResult: null,
        },
      ],
      hasNextPage: false,
    },
  });
  render(<JobDetailClient jobId="job" />);
  expect(screen.queryByRole("link", { name: "Ver cobrança vinculada" })).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Copiar Key" })).not.toBeInTheDocument();
  expect(screen.getByText("charge:123")).toBeInTheDocument();
  expect(screen.getByText("Criar cobrança no sistema financeiro externo - TYPE")).toBeInTheDocument();
  expect(screen.getByText('"jobParameter":').tagName).toBe("STRONG");
  expect(screen.getByText('"jobParameter":').parentElement).toHaveTextContent(
    '"jobParameter": "parameter-value"',
  );
  expect(screen.getByText("Key:").tagName).toBe("STRONG");
  expect(screen.getByText("Key:").parentElement).toHaveTextContent("Key: charge:123");
  expect(screen.getByRole("table", { name: "Execuções do Job" })).toBeInTheDocument();
  expect(screen.getByText('"processed":').parentElement).toHaveTextContent('"processed": 1');
  expect(screen.getByText('"diagnostic":').parentElement).toHaveTextContent(
    '"diagnostic": "diagnostic-value"',
  );
  expect(screen.queryByText("Resultado detalhado indisponível.")).not.toBeInTheDocument();
  expect(screen.getByText("Ocorreu uma falha durante a execução.")).toBeInTheDocument();
});
it("shows waiting with no execution and no charge link", () => {
  mock.detail.mockReturnValue({
    ...query,
    data: { id: "job", type: "TYPE", key: "key", status: "Pending", chargeId: null },
  });
  mock.executions.mockReturnValue({ ...query, data: { items: [], hasNextPage: false } });
  render(<JobDetailClient jobId="job" />);
  expect(screen.getByText("Ainda não há execução registrada.")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Ver cobrança vinculada" })).not.toBeInTheDocument();
});
function jobWithStatus(status: string) {
  mock.detail.mockReturnValue({ ...query, data: { id: "job", type: "TYPE", key: "key", status } });
  mock.executions.mockReturnValue({ ...query, data: { items: [], hasNextPage: false } });
}
it("blocks a manual refresh until both requests finish and releases it after failure", async () => {
  jobWithStatus("Failed");
  let resolveDetail!: () => void;
  let rejectExecutions!: (reason: Error) => void;
  mock.detail.mockReturnValue({ ...query, data: { id: "job", type: "TYPE", key: "key", status: "Failed" },
    refetch: () => new Promise<void>((resolve) => { resolveDetail = resolve; }) });
  mock.executions.mockReturnValue({ ...query, data: { items: [], hasNextPage: false },
    refetch: () => new Promise<void>((_, reject) => { rejectExecutions = reject; }) });
  render(<JobDetailClient jobId="job" />);
  fireEvent.click(screen.getByRole("button", { name: "Atualizar" }));
  expect(screen.getByRole("dialog", { name: "Atualizando detalhes do Job..." })).toBeVisible();
  await act(async () => { resolveDetail(); });
  expect(screen.getByRole("dialog", { name: "Atualizando detalhes do Job..." })).toBeVisible();
  await act(async () => { rejectExecutions(new Error("simulated failure")); });
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Atualizando detalhes do Job..." })).not.toBeInTheDocument());
});
it.each(["Pending", "Processing", "Success"])("does not offer reprocess for %s", (status) => {
  jobWithStatus(status);
  render(<JobDetailClient jobId="job" />);
  expect(screen.queryByRole("button", { name: "Reprocessar" })).not.toBeInTheDocument();
});
it("confirms a failed job explicitly and reports only scheduling success", async () => {
  jobWithStatus("Failed");
  render(<JobDetailClient jobId="job" />);
  fireEvent.click(screen.getByRole("button", { name: "Reprocessar" }));
  const dialog = within(screen.getByRole("dialog"));
  expect(mock.submit).not.toHaveBeenCalled();
  fireEvent.click(dialog.getByRole("button", { name: "Reprocessar" }));
  await waitFor(() =>
    expect(screen.getByText("Job enviado para reprocessamento.")).toBeInTheDocument(),
  );
  expect(mock.submit).toHaveBeenCalledOnce();
});
it("does not submit twice while the first confirmation is pending", async () => {
  jobWithStatus("Failed");
  let resolve!: (value: { message: string }) => void;
  mock.submit.mockImplementation(
    () =>
      new Promise((done) => {
        resolve = done;
      }),
  );
  render(<JobDetailClient jobId="job" />);
  fireEvent.click(screen.getByRole("button", { name: "Reprocessar" }));
  const button = within(screen.getByRole("dialog")).getByRole("button", { name: "Reprocessar" });
  fireEvent.click(button);
  fireEvent.click(button);
  expect(mock.submit).toHaveBeenCalledOnce();
  resolve({ message: "Job enviado para reprocessamento." });
  await waitFor(() =>
    expect(screen.getByText("Job enviado para reprocessamento.")).toBeInTheDocument(),
  );
});
it.each([
  [
    new ApiError(
      "Conflict",
      "O estado deste Job mudou. Atualize os dados antes de tentar novamente.",
      409,
    ),
    "O estado deste Job mudou. Atualize os dados antes de tentar novamente.",
  ],
  [
    new Error("secret network detail"),
    "Não foi possível confirmar o reprocessamento. Atualize os dados para verificar o estado do Job.",
  ],
])("shows a safe failure without resubmitting", async (error, message) => {
  jobWithStatus("Failed");
  mock.submit.mockRejectedValue(error);
  render(<JobDetailClient jobId="job" />);
  fireEvent.click(screen.getByRole("button", { name: "Reprocessar" }));
  fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Reprocessar" }));
  await waitFor(() => expect(screen.getByText(message as string)).toBeInTheDocument());
  expect(mock.submit).toHaveBeenCalledOnce();
  expect(screen.queryByText("secret network detail")).not.toBeInTheDocument();
});
