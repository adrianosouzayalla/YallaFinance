import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import type { JobFilters, JobPage } from "../types/job.types";
import { JobsClient } from "./JobsClient";

const api = vi.hoisted(() => ({ list: vi.fn(), types: vi.fn() }));
vi.mock("../api/jobs.client", () => ({ jobsApi: api }));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ tenantId: "tenant", contextId: "session" }),
}));

let client: QueryClient;
beforeEach(() => {
  vi.clearAllMocks();
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  api.types.mockResolvedValue([
    { type: "TYPE", jobTitle: "Tipo amigável" },
    { type: "NEW_TYPE", jobTitle: "Outro tipo" },
  ]);
  api.list.mockImplementation(async (filters: JobFilters): Promise<JobPage> => ({
    items: [
      {
        id: "job-1",
        key: "charge:fixture",
        type: filters.type || "TYPE",
        jobTitle: "Tipo amigável",
        status: filters.status || "Pending",
        createdAt: "2026-10-08T12:00:00Z",
        updatedAt: "2026-10-08T12:00:00Z",
        startedAt: null,
        finishedAt: null,
        durationSeconds: null,
      },
    ],
    page: filters.page,
    pageSize: filters.pageSize,
    totalItems: filters.status
      ? { Pending: 20, Processing: 25, Success: 80, Failed: 7 }[filters.status]
      : 132,
    summary: { pendingCount: 20, processingCount: 25, successCount: 80, failedCount: 7 },
  }));
});
afterEach(() => {
  cleanup();
  client.clear();
});

async function renderJobs() {
  render(
    <QueryClientProvider client={client}>
      <JobsClient />
    </QueryClientProvider>,
  );
  await screen.findByText("charge:fixture");
}

function indicators() {
  return within(screen.getByRole("region", { name: "Resumo dos Jobs" }));
}

async function expectSearch(filters: Partial<JobFilters>, calls: number) {
  await waitFor(() => expect(api.list).toHaveBeenCalledTimes(calls));
  expect(api.list.mock.lastCall?.[0]).toMatchObject({
    key: "",
    type: "",
    status: "",
    page: 1,
    pageSize: 20,
    ...filters,
  });
  await waitFor(() => expect(screen.getByRole("button", { name: "Atualizar" })).toBeEnabled());
}

it.each([
  ["Aguardando", "Pending"],
  ["Em processamento", "Processing"],
  ["Concluídos", "Success"],
  ["Falhas", "Failed"],
] as const)(
  "clicking %s immediately searches its status and marks the indicator",
  async (label, status) => {
    await renderJobs();

    fireEvent.click(indicators().getByRole("button", { name: new RegExp(`^${label}`) }));

    await expectSearch({ status }, 2);
    expect(screen.getByLabelText("Status")).toHaveValue(status);
    expect(indicators().getByRole("button", { pressed: true })).toHaveAccessibleName(
      new RegExp(`^${label}`),
    );
    expect(indicators().getAllByRole("button", { pressed: false })).toHaveLength(3);
  },
);

it("preserves applied Key and Type, leaves drafts unapplied and returns to page one", async () => {
  await renderJobs();
  fireEvent.change(screen.getByLabelText("Key"), { target: { value: "  charge:123  " } });
  fireEvent.change(screen.getByLabelText("Tipo"), { target: { value: "TYPE" } });
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  await expectSearch({ key: "charge:123", type: "TYPE" }, 2);
  fireEvent.click(screen.getByRole("button", { name: "Go to next page" }));
  await expectSearch({ key: "charge:123", type: "TYPE", page: 2 }, 3);

  fireEvent.change(screen.getByLabelText("Key"), { target: { value: "unapplied" } });
  fireEvent.change(screen.getByLabelText("Tipo"), { target: { value: "NEW_TYPE" } });
  fireEvent.click(indicators().getByRole("button", { name: /^Falhas/ }));

  await expectSearch({ key: "charge:123", type: "TYPE", status: "Failed" }, 4);
  expect(screen.getByLabelText("Status")).toHaveValue("Failed");
  expect(screen.getByLabelText("Key")).toHaveValue("unapplied");
  expect(screen.getByLabelText("Tipo")).toHaveValue("NEW_TYPE");
  expect(indicators().getByRole("button", { name: /^Aguardando/ })).toHaveTextContent("20");
  expect(indicators().getByRole("button", { name: /^Em processamento/ })).toHaveTextContent("25");
  expect(indicators().getByRole("button", { name: /^Concluídos/ })).toHaveTextContent("80");
});

it("clicking the active indicator refreshes the same status immediately", async () => {
  await renderJobs();
  fireEvent.click(indicators().getByRole("button", { name: /^Falhas/ }));
  await expectSearch({ status: "Failed" }, 2);

  fireEvent.click(indicators().getByRole("button", { pressed: true }));

  await expectSearch({ status: "Failed" }, 3);
  expect(screen.getByLabelText("Status")).toHaveValue("Failed");
});

it("clicking the active status on another page fetches page one instead of refreshing the old page", async () => {
  await renderJobs();
  fireEvent.click(indicators().getByRole("button", { name: /^Concluídos/ }));
  await expectSearch({ status: "Success" }, 2);
  fireEvent.click(screen.getByRole("button", { name: "Go to next page" }));
  await expectSearch({ status: "Success", page: 2 }, 3);

  fireEvent.click(indicators().getByRole("button", { pressed: true }));

  await expectSearch({ status: "Success" }, 4);
});

it("marks the applied status when using Filtrar and clears the selection with Limpar", async () => {
  await renderJobs();
  fireEvent.click(indicators().getByRole("button", { name: /^Falhas/ }));
  await expectSearch({ status: "Failed" }, 2);

  fireEvent.change(screen.getByLabelText("Status"), { target: { value: "Pending" } });
  expect(indicators().getByRole("button", { pressed: true })).toHaveAccessibleName(/^Falhas/);
  fireEvent.click(screen.getByRole("button", { name: "Filtrar" }));
  await expectSearch({ status: "Pending" }, 3);
  expect(indicators().getByRole("button", { pressed: true })).toHaveAccessibleName(/^Aguardando/);

  fireEvent.click(screen.getByRole("button", { name: "Limpar" }));
  await expectSearch({}, 4);
  expect(screen.getByLabelText("Status")).toHaveValue("__all_statuses__");
  expect(indicators().queryByRole("button", { pressed: true })).not.toBeInTheDocument();
});

it("allows choosing indicators with Enter and Space", async () => {
  const user = userEvent.setup();
  await renderJobs();
  indicators()
    .getByRole("button", { name: /^Em processamento/ })
    .focus();

  await user.keyboard("[Enter]");
  await expectSearch({ status: "Processing" }, 2);

  indicators()
    .getByRole("button", { name: /^Falhas/ })
    .focus();
  await user.keyboard(" ");
  await expectSearch({ status: "Failed" }, 3);
});

it("blocks a manual refresh until its response and leaves background refreshes usable", async () => {
  await renderJobs();
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  const initial = await api.list.mock.results[0].value as JobPage;
  let resolveRefresh!: (page: JobPage) => void;
  api.list.mockImplementationOnce(() => new Promise<JobPage>((resolve) => { resolveRefresh = resolve; }));
  fireEvent.click(screen.getByRole("button", { name: "Atualizar" }));
  expect(await screen.findByRole("dialog")).toHaveTextContent("Atualizando Jobs...");
  resolveRefresh(initial);
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  expect(screen.getByRole("button", { name: "Atualizar" })).toBeEnabled();

  let resolveBackground!: (page: JobPage) => void;
  api.list.mockImplementationOnce(() => new Promise<JobPage>((resolve) => { resolveBackground = resolve; }));
  const refresh = client.invalidateQueries({ queryKey: ["jobs"] });
  await waitFor(() => expect(resolveBackground).toBeDefined());
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(screen.getByRole("table", { name: "Jobs" })).toBeVisible();
  resolveBackground(initial);
  await refresh;
});
