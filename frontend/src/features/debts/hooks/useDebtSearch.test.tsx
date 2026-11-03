import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { expect, it, vi } from "vitest";
import { createQueryClient } from "@/providers/QueryProvider";
import { SessionProvider } from "@/features/auth/context/SessionProvider";
import { debtsApi } from "../api/debts.client";
import type { DebtFilters, DebtListResult } from "../types/debt.types";
import { useDebtSearch } from "./useDebtSearch";
vi.mock("../api/debts.client", () => ({ debtsApi: { listDebts: vi.fn() } }));
const filters: DebtFilters = {
  page: 1,
  pageSize: 5,
  categories: [],
  status: "Todos",
  startDate: "",
  endDate: "",
  sortDirection: "desc",
};
const response = (page: number): DebtListResult => ({
  items: [],
  page,
  pageSize: 5,
  totalItems: page,
  summary: { openCount: 0, openAmount: 0, chargingAmount: 0, paidAmount: 0 },
});

it("keeps the current filter response when an earlier request finishes late", async () => {
  const client = createQueryClient();
  let resolveEarlier!: (data: DebtListResult) => void;
  const earlier = new Promise<DebtListResult>((resolve) => {
    resolveEarlier = resolve;
  });
  vi.mocked(debtsApi.listDebts).mockImplementation((value) =>
    value.page === 1 ? earlier : Promise.resolve(response(2)),
  );
  const wrapper = ({ children }: PropsWithChildren) => (
    <QueryClientProvider client={client}>
      <SessionProvider session={{ tenantId: "a", contextId: "session-a" }}>
        {children}
      </SessionProvider>
    </QueryClientProvider>
  );
  const hook = renderHook(useDebtSearch, { wrapper });
  act(() => {
    void hook.result.current.load(filters).catch(() => undefined);
  });
  await waitFor(() => expect(debtsApi.listDebts).toHaveBeenCalled());
  await act(async () => {
    await hook.result.current.load({ ...filters, page: 2 });
  });
  await waitFor(() => expect(hook.result.current.data?.page).toBe(2));
  await act(async () => {
    resolveEarlier(response(1));
    await earlier;
  });
  expect(hook.result.current.data?.page).toBe(2);
});

it("isolates the same filters between sessions and clears the previous session cache", async () => {
  const client = createQueryClient();
  let session = { tenantId: "a", contextId: "session-a" };
  vi.mocked(debtsApi.listDebts).mockResolvedValue(response(1));
  const wrapper = ({ children }: PropsWithChildren) => (
    <QueryClientProvider client={client}>
      <SessionProvider session={session}>{children}</SessionProvider>
    </QueryClientProvider>
  );
  const hook = renderHook(useDebtSearch, { wrapper });
  await act(async () => {
    await hook.result.current.load(filters);
  });
  await waitFor(() => expect(hook.result.current.data?.page).toBe(1));
  session = { tenantId: "b", contextId: "session-b" };
  vi.mocked(debtsApi.listDebts).mockResolvedValue(response(2));
  hook.rerender();
  expect(hook.result.current.data?.page).not.toBe(1);
  await waitFor(() => expect(hook.result.current.data?.page).toBe(2));
  expect(
    client
      .getQueryCache()
      .getAll()
      .some((query) => query.queryKey[2] === "session-a"),
  ).toBe(false);
  expect(
    client
      .getQueryCache()
      .getAll()
      .every((query) => query.queryKey[1] === "b"),
  ).toBe(true);
});
