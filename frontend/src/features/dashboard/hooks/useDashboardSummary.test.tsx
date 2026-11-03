import { act, cleanup, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { useDashboardSummary } from "./useDashboardSummary";

const mocks = vi.hoisted(() => ({ summary: vi.fn() }));
vi.mock("../api/dashboard.client", () => ({ dashboardApi: { getDashboardSummary: mocks.summary } }));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ tenantId: "tenant", contextId: "session" }),
}));

const data = {
  awaitingCharge: { count: 4, amount: 150 },
  activeCharges: { count: 2, amount: 30 },
  overdue: { count: 1, amount: 10 },
  staleCharges: { count: 0, amount: 0 },
};
let client: QueryClient;
function wrapper({ children }: PropsWithChildren) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
beforeEach(() => {
  client = new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } } });
  vi.useFakeTimers();
  Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
  mocks.summary.mockReset().mockResolvedValue(data);
});
afterEach(() => {
  cleanup();
  client.clear();
  vi.useRealTimers();
});

it("refreshes every thirty seconds, pauses while hidden and resumes when visible", async () => {
  renderHook(() => useDashboardSummary(), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  expect(mocks.summary).toHaveBeenCalledTimes(1);
  await act(() => vi.advanceTimersByTimeAsync(29000));
  expect(mocks.summary).toHaveBeenCalledTimes(1);
  await act(() => vi.advanceTimersByTimeAsync(1000));
  expect(mocks.summary).toHaveBeenCalledTimes(2);
  await act(async () => {
    Object.defineProperty(document, "visibilityState", { configurable: true, value: "hidden" });
    document.dispatchEvent(new Event("visibilitychange"));
  });
  await act(() => vi.advanceTimersByTimeAsync(60000));
  expect(mocks.summary).toHaveBeenCalledTimes(2);
  await act(async () => {
    Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
    document.dispatchEvent(new Event("visibilitychange"));
  });
  await act(() => vi.advanceTimersByTimeAsync(30000));
  expect(mocks.summary).toHaveBeenCalledTimes(3);
});

it("preserves indicators during a slow refresh, without overlapping requests", async () => {
  mocks.summary.mockResolvedValueOnce(data).mockImplementation(() => new Promise(() => {}));
  const hook = renderHook(() => useDashboardSummary(), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  await act(() => vi.advanceTimersByTimeAsync(30000));
  expect(hook.result.current.isFetching).toBe(true);
  expect(hook.result.current.data).toEqual(data);
  await act(() => vi.advanceTimersByTimeAsync(90000));
  expect(mocks.summary).toHaveBeenCalledTimes(2);
});

it("preserves the last indicators when the refresh fails", async () => {
  mocks.summary.mockResolvedValueOnce(data).mockRejectedValue(new Error("offline"));
  const hook = renderHook(() => useDashboardSummary(), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  await act(() => vi.advanceTimersByTimeAsync(30050));
  expect(hook.result.current.isError).toBe(true);
  expect(hook.result.current.data).toEqual(data);
});

it("aborts the request and stops polling when leaving the page", async () => {
  let signal: AbortSignal | undefined;
  mocks.summary.mockImplementation((options) => {
    signal = options.signal;
    return new Promise(() => {});
  });
  const hook = renderHook(() => useDashboardSummary(), { wrapper });
  await act(() => vi.advanceTimersByTimeAsync(50));
  hook.unmount();
  expect(signal?.aborted).toBe(true);
  await act(() => vi.advanceTimersByTimeAsync(60000));
  expect(mocks.summary).toHaveBeenCalledTimes(1);
});
