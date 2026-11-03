import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { JobExecutionsPanel } from "./JobExecutionsPanel";
import type { JobExecutionPage } from "../types/job.types";
afterEach(cleanup);
it("shows persisted attempt metadata and opens the selected execution JSON", () => {
  const data: JobExecutionPage = {
    page: 1,
    pageSize: 20,
    totalItems: 2,
    hasNextPage: false,
    items: [
      {
        id: "exec-a",
        status: "Success",
        startedAt: "2026-10-05T12:00:00Z",
        finishedAt: "2026-10-05T12:00:01Z",
        durationSeconds: 1,
        errorCode: null,
        result: { attempt: 1 },
        details: null,
      },
      {
        id: "exec-b",
        status: "Failed",
        startedAt: "2026-10-05T12:01:00Z",
        finishedAt: "2026-10-05T12:01:02Z",
        durationSeconds: 2,
        errorCode: "JobHandlerError",
        result: null,
        details: { attempt: 2 },
      },
    ],
  };
  render(
    <JobExecutionsPanel
      data={data}
      page={1}
      onPageChange={vi.fn()}
      error={false}
      onRefresh={vi.fn()}
    />,
  );
  const table = within(screen.getByRole("table", { name: "Execuções do Job" }));
  expect(table.getByText("Sucesso")).toBeInTheDocument();
  expect(table.getByRole("cell", { name: "Erro" })).toBeInTheDocument();
  expect(screen.getByText('"attempt":').parentElement).toHaveTextContent('"attempt": 1');
  expect(screen.getByText("Execução:").tagName).toBe("STRONG");
  expect(screen.getByText("Execução:").parentElement).toHaveTextContent("Execução: exec-a");
  fireEvent.click(table.getAllByRole("button", { name: "Ver detalhes" })[1]);
  expect(screen.getByText('"attempt":').parentElement).toHaveTextContent('"attempt": 2');
  expect(screen.getByText('"attempt":').parentElement).not.toHaveTextContent('"attempt": 1');
});
it("lets the user reach and inspect a newer execution on a later history page", () => {
  const onPageChange = vi.fn();
  const old = {
    id: "old",
    status: "Failed" as const,
    startedAt: "2026-10-05T12:00:00Z",
    finishedAt: "2026-10-05T12:00:01Z",
    durationSeconds: 1,
    errorCode: "JobHandlerError",
    result: { attempt: 1 },
    details: null,
  };
  const props = {
    page: 1,
    onPageChange,
    error: false,
    onRefresh: vi.fn(),
  };
  const first: JobExecutionPage = {
    items: [old],
    page: 1,
    pageSize: 1,
    totalItems: 2,
    hasNextPage: true,
  };
  const view = render(<JobExecutionsPanel {...props} data={first} />);
  fireEvent.click(screen.getByRole("button", { name: "Go to next page" }));
  expect(onPageChange).toHaveBeenCalledWith(2);
  view.rerender(
    <JobExecutionsPanel
      {...props}
      page={2}
      data={{
        ...first,
        page: 2,
        hasNextPage: false,
        items: [{ ...old, id: "new", status: "Success", errorCode: null, result: { attempt: 2 } }],
      }}
    />,
  );
  expect(screen.getByText("Execução:").parentElement).toHaveTextContent("Execução: new");
  expect(screen.getByText('"attempt":').parentElement).toHaveTextContent('"attempt": 2');
});

it("shows the provider's safe error message without opening the JSON details", () => {
  const data: JobExecutionPage = {
    page: 1, pageSize: 20, totalItems: 1, hasNextPage: false,
    items: [{
      id: "exec-financial", status: "Failed", startedAt: "2026-10-20T21:06:00Z",
      finishedAt: "2026-10-20T21:06:02Z", durationSeconds: 2,
      errorCode: "ChargeSettlementFailed", result: null,
      details: { items: [{ diagnostic: { providerErrorCode: "validation_error", providerErrorMessage: "Conta corrente não encontrada" } }] },
    }],
  };
  render(<JobExecutionsPanel data={data} page={1} onPageChange={vi.fn()} error={false} onRefresh={vi.fn()} />);
  expect(screen.getByText("Conta corrente não encontrada")).toBeInTheDocument();
  expect(screen.getByText("Código: validation_error")).toBeInTheDocument();
});

it("shows a prior manual provider reproduction when the selected retry has no provider response", () => {
  const data: JobExecutionPage = {
    page: 1, pageSize: 20, totalItems: 2, hasNextPage: false,
    items: [
      {
        id: "latest", status: "Failed", startedAt: "2026-10-21T14:43:02Z",
        finishedAt: "2026-10-21T14:43:04Z", durationSeconds: 2,
        errorCode: "ChargeSettlementIndeterminate",
        result: { items: [{ diagnostic: null }] }, details: { items: [{ diagnostic: null }] },
      },
      {
        id: "original", status: "Failed", startedAt: "2026-10-20T21:06:10Z",
        finishedAt: "2026-10-20T21:06:12Z", durationSeconds: 2,
        errorCode: "ChargeSettlementIndeterminate", result: null,
        details: { manualReproduction: {
          source: "manual-reproduction", providerErrorCode: "validation_error",
          providerErrorMessage: "Conta corrente não encontrada",
        } },
      },
    ],
  };
  render(<JobExecutionsPanel data={data} page={1} onPageChange={vi.fn()} error={false} onRefresh={vi.fn()} />);
  expect(screen.getByText("Última reprodução manual da integração financeira")).toBeInTheDocument();
  expect(screen.getByText("Conta corrente não encontrada")).toBeInTheDocument();
  expect(screen.getByText("Execução de origem: original")).toBeInTheDocument();
});
