import { act, cleanup, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { useJobs } from "./useJobs";
import { useJob } from "./useJob";
const mocks = vi.hoisted(() => ({ list: vi.fn(), detail: vi.fn(), executions: vi.fn() }));
vi.mock("../api/jobs.client", () => ({ jobsApi: mocks }));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ tenantId: "tenant", contextId: "session" }),
}));
let client: QueryClient;
function wrapper({ children }: PropsWithChildren) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
const filters = { key: "", type: "", status: "" as const, page: 1, pageSize: 20 };
beforeEach(() => {
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  vi.useFakeTimers();
  Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
  mocks.list.mockReset().mockResolvedValue({ items: [], page: 1, pageSize: 20, totalItems: 0 });
  mocks.detail.mockReset().mockResolvedValue({ id: "job", status: "Processing" });
});
afterEach(() => {
  cleanup();
  vi.useRealTimers();
});
it("polls every ten seconds and suspends when hidden", async () => {
  renderHook(() => useJobs(filters), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  expect(mocks.list).toHaveBeenCalledTimes(1);
  await act(() => vi.advanceTimersByTimeAsync(10000));
  expect(mocks.list).toHaveBeenCalledTimes(2);
  await act(async () => {
    Object.defineProperty(document, "visibilityState", { configurable: true, value: "hidden" });
    document.dispatchEvent(new Event("visibilitychange"));
  });
  await act(() => vi.advanceTimersByTimeAsync(20000));
  expect(mocks.list).toHaveBeenCalledTimes(2);
});
it("does not overlap a slow request and aborts it on leaving", async () => {
  let signal: AbortSignal | undefined;
  mocks.list.mockImplementation((_filters, options) => {
    signal = options.signal;
    return new Promise(() => {});
  });
  const hook = renderHook(() => useJobs(filters), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(30000));
  expect(mocks.list).toHaveBeenCalledTimes(1);
  hook.unmount();
  expect(signal?.aborted).toBe(true);
});
it("stops polling a finalized detail", async () => {
  mocks.detail.mockResolvedValue({ id: "job", status: "Success" });
  renderHook(() => useJob("job"), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  await act(() => vi.advanceTimersByTimeAsync(30000));
  expect(mocks.detail).toHaveBeenCalledTimes(1);
});
it("cancels a previous filter and never replaces the new result with its late response", async () => {
  let resolve!: (value: unknown) => void;
  let oldSignal: AbortSignal | undefined;
  mocks.list
    .mockImplementationOnce((_filters, options) => {
      oldSignal = options.signal;
      return new Promise((done) => {
        resolve = done;
      });
    })
    .mockResolvedValue({ items: [], totalItems: 7, page: 1, pageSize: 20 });
  const hook = renderHook(({ key }) => useJobs({ ...filters, key }), {
    wrapper,
    initialProps: { key: "old" },
  });
  await act(() => vi.advanceTimersByTimeAsync(50));
  hook.rerender({ key: "new" });
  await act(() => vi.advanceTimersByTimeAsync(50));
  expect(oldSignal?.aborted).toBe(true);
  await act(async () => {
    resolve({ items: [], totalItems: 99 });
    await vi.advanceTimersByTimeAsync(50);
  });
  expect(hook.result.current.data?.totalItems).toBe(7);
});
it("preserves the received rows when an update fails", async () => {
  mocks.list
    .mockResolvedValueOnce({ items: [], totalItems: 3, page: 1, pageSize: 20 })
    .mockRejectedValue(new Error("offline"));
  const hook = renderHook(() => useJobs(filters), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  await act(() => vi.advanceTimersByTimeAsync(10050));
  expect(hook.result.current.isError).toBe(true);
  expect(hook.result.current.data?.totalItems).toBe(3);
});
