import { act, cleanup, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { useJobExecutions } from "./useJobExecutions";
import type { JobStatus } from "../types/job.types";
const mock = vi.hoisted(() => ({ executions: vi.fn() }));
vi.mock("../api/jobs.client", () => ({ jobsApi: mock }));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ tenantId: "tenant", contextId: "session" }),
}));
afterEach(() => {
  cleanup();
  vi.useRealTimers();
});
it("fetches final executions after an older request completes without overlap", async () => {
  vi.useFakeTimers();
  let resolve!: (value: unknown) => void;
  mock.executions
    .mockReset()
    .mockImplementationOnce(
      () =>
        new Promise((done) => {
          resolve = done;
        }),
    )
    .mockResolvedValue({ items: [{ finishedAt: "2026-10-05T12:00:00Z" }], hasNextPage: false });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: PropsWithChildren) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  const hook = renderHook(
    ({ status }: { status: JobStatus }) => useJobExecutions("job", 1, status),
    { wrapper: Wrapper, initialProps: { status: "Processing" } },
  );
  await act(() => vi.advanceTimersByTimeAsync(50));
  hook.rerender({ status: "Success" });
  await act(() => vi.advanceTimersByTimeAsync(50));
  expect(mock.executions).toHaveBeenCalledTimes(1);
  await act(async () => {
    resolve({ items: [{ finishedAt: null }], hasNextPage: false });
    await vi.advanceTimersByTimeAsync(100);
  });
  expect(mock.executions).toHaveBeenCalledTimes(2);
  await act(() => vi.advanceTimersByTimeAsync(50));
  expect(hook.result.current.data?.items[0].finishedAt).not.toBeNull();
});
