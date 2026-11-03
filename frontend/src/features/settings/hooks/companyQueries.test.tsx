import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { beforeEach, expect, it, vi } from "vitest";
import { companyApi } from "../company.client";
import { useCompanyConnections, useCompanySettings } from "./useCompanySettings";
import { useCompanyConnection } from "./useCompanyConnection";
import { DEFAULT_CONNECTION_FILTERS, DEFAULT_DETAIL_FILTERS } from "../types";
import { request } from "@/services/api/api-client";
const session = vi.hoisted(() => ({ tenantId: "tenant-a", contextId: "context-a" }));
vi.mock("@/features/auth/context/SessionProvider", () => ({ useSessionContext: () => session }));
vi.mock("../company.client", () => ({
  companyApi: { getSettings: vi.fn(), listConnections: vi.fn(), getConnection: vi.fn() },
}));
vi.mock("@/services/api/api-client", () => ({ request: vi.fn() }));
let client: QueryClient;
beforeEach(() => {
  vi.resetAllMocks();
  session.tenantId = "tenant-a";
  session.contextId = "context-a";
  client = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } });
});
function wrapper({ children }: PropsWithChildren) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
it("loads groups only on demand and uses memory cache", async () => {
  vi.mocked(companyApi.getSettings).mockResolvedValue({
    maximumLinkDays: "invalid",
    publicOrigin: null,
  });
  const hook = renderHook(({ enabled }) => useCompanySettings("payments", enabled), {
    wrapper,
    initialProps: { enabled: false },
  });
  expect(companyApi.getSettings).not.toHaveBeenCalled();
  hook.rerender({ enabled: true });
  await waitFor(() => expect(hook.result.current.data?.maximumLinkDays).toBe("invalid"));
  hook.rerender({ enabled: false });
  hook.rerender({ enabled: true });
  expect(companyApi.getSettings).toHaveBeenCalledTimes(1);
  expect(client.getQueryCache().getAll()[0].queryKey).toEqual([
    "company",
    "tenant-a",
    "context-a",
    "settings",
    "payments",
  ]);
});
it("keeps list data for paging but clears it immediately when context changes", async () => {
  const page = { items: [], page: 1, pageSize: 20, totalItems: 1 };
  vi.mocked(companyApi.listConnections)
    .mockResolvedValueOnce(page)
    .mockImplementation(() => new Promise(() => {}));
  const hook = renderHook(
    ({ page }) => useCompanyConnections({ ...DEFAULT_CONNECTION_FILTERS, page }, true),
    { wrapper, initialProps: { page: 1 } },
  );
  await waitFor(() => expect(hook.result.current.data).toEqual(page));
  hook.rerender({ page: 2 });
  expect(hook.result.current.data).toEqual(page);
  session.contextId = "context-b";
  hook.rerender({ page: 2 });
  expect(hook.result.current.data).toBeUndefined();
});
it("does not query a new connection without id and cancels stale detail requests", async () => {
  let oldSignal: AbortSignal | undefined;
  let finishOld!: (value: never) => void;
  vi.mocked(companyApi.getConnection)
    .mockImplementationOnce((_id, _filter, options) => {
      oldSignal = options.signal;
      return new Promise((resolve) => {
        finishOld = resolve;
      });
    })
    .mockResolvedValue({ connection: { id: "new" } } as never);
  const hook = renderHook(
    ({ id }: { id: string | null }) => useCompanyConnection(id, DEFAULT_DETAIL_FILTERS, true),
    { wrapper, initialProps: { id: null as string | null } },
  );
  expect(companyApi.getConnection).not.toHaveBeenCalled();
  hook.rerender({ id: "old" });
  await waitFor(() => expect(companyApi.getConnection).toHaveBeenCalledTimes(1));
  hook.rerender({ id: "new" });
  await waitFor(() => expect(hook.result.current.data?.connection.id).toBe("new"));
  expect(oldSignal?.aborted).toBe(true);
  await act(async () => finishOld({ connection: { id: "old" } } as never));
  expect(hook.result.current.data?.connection.id).toBe("new");
});
it("does not retry failed settings automatically", async () => {
  vi.mocked(companyApi.getSettings).mockRejectedValue(new Error("Unavailable"));
  const hook = renderHook(() => useCompanySettings("session", true), { wrapper });
  await waitFor(() => expect(hook.result.current.isError).toBe(true));
  expect(companyApi.getSettings).toHaveBeenCalledTimes(1);
  await act(async () => {
    await hook.result.current.refetch();
  });
  expect(companyApi.getSettings).toHaveBeenCalledTimes(2);
});
it("keeps detail on pagination only for the same connection and context", async () => {
  const detail = { connection: { id: "one" } };
  vi.mocked(companyApi.getConnection)
    .mockResolvedValueOnce(detail as never)
    .mockImplementation(() => new Promise(() => {}));
  const hook = renderHook(
    ({ id, page }) =>
      useCompanyConnection(id, { ...DEFAULT_DETAIL_FILTERS, categoryPage: page }, true),
    {
      wrapper,
      initialProps: { id: "one", page: 1 },
    },
  );
  await waitFor(() => expect(hook.result.current.data).toEqual(detail));
  hook.rerender({ id: "one", page: 2 });
  expect(hook.result.current.data).toEqual(detail);
  session.tenantId = "tenant-b";
  session.contextId = "context-b";
  hook.rerender({ id: "one", page: 2 });
  expect(hook.result.current.data).toBeUndefined();
  hook.rerender({ id: "two", page: 2 });
  expect(hook.result.current.data).toBeUndefined();
});
it("client forwards context and cancellation through GET-only relative URLs", async () => {
  const { companyApi: actualApi } =
    await vi.importActual<typeof import("../company.client")>("../company.client");
  const signal = new AbortController().signal;
  const options = { contextId: "context", signal };
  await actualApi.getSettings("user-registration", options);
  await actualApi.listConnections(
    { ...DEFAULT_CONNECTION_FILTERS, search: "a & b", enabled: false },
    options,
  );
  await actualApi.getConnection("connection", DEFAULT_DETAIL_FILTERS, options);
  expect(request).toHaveBeenNthCalledWith(1, "/api/company/settings/user-registration", {
    headers: { "X-Yalla-Context": "context" },
    signal,
  });
  expect(vi.mocked(request).mock.calls[1][0]).toContain("search=a+%26+b&enabled=false");
  expect(vi.mocked(request).mock.calls[2][0]).toContain(
    "/api/company/connections/connection?categoryPage=1",
  );
  for (const [, init] of vi.mocked(request).mock.calls) {
    expect(init?.method).toBeUndefined();
    expect(init?.signal).toBe(signal);
  }
});
