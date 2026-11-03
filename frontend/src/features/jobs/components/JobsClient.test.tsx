import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { JobsClient } from "./JobsClient";
const mock = vi.hoisted(() => ({ hook: vi.fn(), refetch: vi.fn() }));
vi.mock("../hooks/useJobs", () => ({ useJobs: mock.hook }));
vi.mock("../hooks/useJobTypes", () => ({
  useJobTypes: () => ({
    data: [
      { type: "TYPE", jobTitle: "Tipo amigável" },
      { type: "NEW_TYPE", jobTitle: "Nova funcionalidade" },
    ],
    isPending: false,
    isError: false,
    refetch: vi.fn(),
  }),
}));
afterEach(cleanup);
it("defaults to today, applies another period and resets to today", () => {
  vi.useFakeTimers();
  vi.setSystemTime(new Date(2026, 9, 15, 12));
  mock.hook.mockReturnValue({ data: { items: [], totalItems: 0 }, isPending: false, isFetching: false, isError: false, refetch: mock.refetch });
  try {
    render(<JobsClient />);
    expect(screen.getByLabelText(/Data início/)).toHaveValue("2026-10-15");
    expect(screen.getByLabelText(/Data fim/)).toHaveValue("2026-10-15");
    expect(mock.hook.mock.lastCall?.[0]).toMatchObject({ startDate: "2026-10-15", endDate: "2026-10-15" });
    fireEvent.change(screen.getByLabelText(/Data início/), { target: { value: "2026-10-10" } });
    fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
    expect(mock.hook.mock.lastCall?.[0]).toMatchObject({ startDate: "2026-10-10", endDate: "2026-10-15", page: 1 });
    fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
    expect(mock.hook.mock.lastCall?.[0]).toMatchObject({ startDate: "2026-10-15", endDate: "2026-10-15" });
  } finally { vi.useRealTimers(); }
});
it("rejects reversed dates without querying or blocking the screen", () => {
  mock.hook.mockReturnValue({ data: { items: [], totalItems: 0 }, isPending: false, isFetching: false, isError: false });
  render(<JobsClient />);
  const applied = mock.hook.mock.lastCall?.[0];
  fireEvent.change(screen.getByLabelText(/Data início/), { target: { value: "2026-10-16" } });
  fireEvent.change(screen.getByLabelText(/Data fim/), { target: { value: "2026-10-15" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  expect(screen.getByRole("alert")).toHaveTextContent("Informe um período válido");
  expect(mock.hook.mock.lastCall?.[0]).toEqual(applied);
  expect(screen.getByRole("button", { name: "Filtrar" })).toBeEnabled();
});
it("requests header sorting from the server and returns to page one", () => {
  mock.hook.mockReturnValue({ data: { items: [], totalItems: 50 }, isPending: false, isFetching: false, isError: false });
  render(<JobsClient />);
  fireEvent.click(screen.getByRole("button", { name: "Go to next page" }));
  fireEvent.click(within(screen.getByRole("columnheader", { name: "Criação" })).getByText("Criação"));
  expect(mock.hook).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, sortBy: "created", sortDirection: "asc" }));
  fireEvent.click(within(screen.getByRole("columnheader", { name: "Criação" })).getByText("Criação"));
  expect(mock.hook).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, sortBy: "created", sortDirection: "desc" }));
});
it("shows totals across pages below the applied filters and refreshes them with the results", () => {
  const unfiltered = { pendingCount: 20, processingCount: 5, successCount: 80, failedCount: 7 };
  const filtered = { pendingCount: 0, processingCount: 0, successCount: 0, failedCount: 3 };
  let summary = unfiltered;
  mock.hook.mockImplementation((filters) => ({
    data: {
      items: [],
      page: filters.page,
      pageSize: 20,
      totalItems: 112,
      summary: filters.key ? filtered : summary,
    },
    isPending: false,
    isFetching: false,
    isError: false,
    refetch: mock.refetch,
  }));
  const view = render(<JobsClient />);
  const indicators = () => screen.getByRole("region", { name: "Resumo dos Jobs" });
  const expectCounts = (pending: string, processing: string, success: string, failed: string) => {
    const region = within(indicators());
    expect(region.getByText("Aguardando").parentElement).toHaveTextContent(`Aguardando${pending}`);
    expect(region.getByText("Em processamento").parentElement).toHaveTextContent(
      `Em processamento${processing}`,
    );
    expect(region.getByText("Concluídos").parentElement).toHaveTextContent(success);
    expect(region.getByText("Falhas").parentElement).toHaveTextContent(failed);
  };
  expectCounts("20", "5", "80", "7");
  const filterButton = screen.getByRole("button", { name: "Filtrar" });
  expect(
    filterButton.compareDocumentPosition(indicators()) & Node.DOCUMENT_POSITION_FOLLOWING,
  ).toBeTruthy();
  expect(
    indicators().compareDocumentPosition(screen.getByRole("table", { name: "Jobs" })) &
      Node.DOCUMENT_POSITION_FOLLOWING,
  ).toBeTruthy();
  fireEvent.click(screen.getByRole("button", { name: "Go to next page" }));
  expectCounts("20", "5", "80", "7");
  fireEvent.change(screen.getByLabelText("Key"), { target: { value: "filtered" } });
  expectCounts("20", "5", "80", "7");
  fireEvent.click(filterButton);
  expectCounts("0", "0", "0", "3");
  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  expectCounts("20", "5", "80", "7");
  summary = { pendingCount: 19, processingCount: 5, successCount: 81, failedCount: 7 };
  view.rerender(<JobsClient />);
  expectCounts("19", "5", "81", "7");
});
it("shows job key and applies partial search only after Filtrar", () => {
  mock.hook.mockReturnValue({
    data: {
      items: [
        {
          id: "job-1",
          key: "charge:123",
          type: "TYPE",
          jobTitle: "Tipo amigável",
          status: "Processing",
          createdAt: "2026-10-05T12:00:00Z",
          startedAt: null,
          durationSeconds: null,
        },
      ],
      totalItems: 1,
    },
    isPending: false,
    isFetching: false,
    isError: false,
    refetch: mock.refetch,
  });
  render(<JobsClient />);
  expect(screen.getByText("charge:123")).toBeInTheDocument();
  expect(
    within(screen.getByRole("table", { name: "Jobs" })).getByText("Tipo amigável"),
  ).toBeInTheDocument();
  expect(screen.getByRole("option", { name: "Nova funcionalidade" })).toHaveValue("NEW_TYPE");
  expect(
    within(screen.getByRole("table", { name: "Jobs" })).getByText("Em processamento"),
  ).toBeInTheDocument();
  expect(screen.queryByText("job-1")).not.toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Ver detalhes" })).toHaveAttribute("href", "/jobs/job-1");
  fireEvent.change(screen.getByLabelText("Tipo"), { target: { value: "NEW_TYPE" } });
  fireEvent.change(screen.getByLabelText("Key"), { target: { value: "123" } });
  expect(mock.hook.mock.lastCall?.[0].key).toBe("");
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  expect(mock.hook.mock.lastCall?.[0]).toMatchObject({ key: "123", type: "NEW_TYPE", page: 1 });
  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  expect(mock.hook.mock.lastCall?.[0].key).toBe("");
  expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
});
it("shows empty and error states without fabricated results", () => {
  mock.hook.mockReturnValue({
    data: undefined,
    isPending: false,
    isFetching: false,
    isError: true,
    refetch: mock.refetch,
  });
  render(<JobsClient />);
  expect(screen.getByText("Não foi possível consultar os Jobs.")).toBeInTheDocument();
  expect(
    within(screen.getByRole("region", { name: "Resumo dos Jobs" })).getAllByText("—"),
  ).toHaveLength(4);
  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
  expect(mock.refetch).toHaveBeenCalled();
});
