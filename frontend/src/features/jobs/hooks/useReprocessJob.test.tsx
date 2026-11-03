import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { ApiError } from "@/services/api/api-error";
import { useReprocessJob } from "./useReprocessJob";
import { useJob } from "./useJob";
import { useJobExecutions } from "./useJobExecutions";
const mock = vi.hoisted(() => ({ reprocess: vi.fn(), detail: vi.fn(), executions: vi.fn() }));
vi.mock("../api/jobs.client", () => ({ jobsApi: mock }));
vi.mock("@/features/auth/context/SessionProvider", () => ({
  useSessionContext: () => ({ tenantId: "tenant", contextId: "session" }),
}));
afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  vi.useRealTimers();
});
function setup() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  function Wrapper({ children }: PropsWithChildren) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return { client, hook: renderHook(() => useReprocessJob("job"), { wrapper: Wrapper }) };
}
it("refreshes tenant scoped queries without inventing Pending and discards an older GET", async () => {
  const { client, hook } = setup();
  const key = ["job", "tenant", "session", "job"];
  const foreign = ["job", "other", "session", "job"];
  client.setQueryData(key, { status: "Failed" });
  client.setQueryData(foreign, { status: "Failed" });
  client.setQueryData(["jobs", "tenant", "session", { key: "filter", page: 2 }], { page: 2 });
  client.setQueryData(["job-executions", "tenant", "session", "job", 2], { items: [] });
  let resolve!: (data: unknown) => void;
  const oldGet = client
    .fetchQuery({
      queryKey: key,
      queryFn: () =>
        new Promise((done) => {
          resolve = done;
        }),
    })
    .catch(() => undefined);
  mock.reprocess.mockResolvedValue({ message: "Job enviado para reprocessamento." });
  await act(async () => {
    await hook.result.current.mutateAsync();
  });
  resolve({ status: "Failed", old: true });
  await oldGet;
  expect(mock.reprocess).toHaveBeenCalledWith("job", { contextId: "session" });
  expect(client.getQueryData(key)).toEqual({ status: "Failed" });
  expect(client.getQueryState(key)?.isInvalidated).toBe(true);
  expect(client.getQueryState(foreign)?.isInvalidated).toBe(false);
  expect(
    client.getQueryState(["job-executions", "tenant", "session", "job", 2])?.isInvalidated,
  ).toBe(true);
  expect(client.getQueryData(["jobs", "tenant", "session", { key: "filter", page: 2 }])).toEqual({
    page: 2,
  });
});
it.each([new ApiError("Conflict", "Estado mudou.", 409), new Error("network")])(
  "never retries a failed POST",
  async (error) => {
    const { hook } = setup();
    mock.reprocess.mockRejectedValue(error);
    await act(async () => {
      await expect(hook.result.current.mutateAsync()).rejects.toBe(error);
    });
    await waitFor(() => expect(hook.result.current.isError).toBe(true));
    expect(mock.reprocess).toHaveBeenCalledTimes(1);
  },
);
it("resumes polling after requeue and obtains the new final execution without changing the previous attempt", async () => {
  vi.useFakeTimers();
  Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
  let status = "Failed";
  const old = { id: "old", status: "Failed", finishedAt: "2026-10-05T12:00:00Z" };
  mock.detail.mockImplementation(async () => ({ id: "job", status }));
  mock.executions.mockImplementation(async () => ({
    items: status === "Success" ? [old, { id: "new", status: "Success" }] : [old],
    totalItems: status === "Success" ? 2 : 1,
  }));
  mock.reprocess.mockImplementation(async () => {
    status = "Pending";
    return { message: "Job enviado para reprocessamento." };
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: PropsWithChildren) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  const hook = renderHook(
    () => {
      const job = useJob("job");
      const executions = useJobExecutions("job", 1, job.data?.status);
      return { job, executions, mutation: useReprocessJob("job") };
    },
    { wrapper: Wrapper },
  );
  await act(() => vi.advanceTimersByTimeAsync(100));
  expect(hook.result.current.job.data?.status).toBe("Failed");
  await act(async () => {
    await hook.result.current.mutation.mutateAsync();
    await vi.advanceTimersByTimeAsync(100);
  });
  expect(hook.result.current.job.data?.status).toBe("Pending");
  expect(hook.result.current.executions.data?.items[0].status).toBe("Failed");
  status = "Processing";
  await act(() => vi.advanceTimersByTimeAsync(10100));
  expect(hook.result.current.job.data?.status).toBe("Processing");
  status = "Success";
  await act(() => vi.advanceTimersByTimeAsync(10100));
  expect(hook.result.current.job.data?.status).toBe("Success");
  expect(hook.result.current.executions.data?.items).toEqual([
    old,
    { id: "new", status: "Success" },
  ]);
  const calls = mock.detail.mock.calls.length;
  await act(() => vi.advanceTimersByTimeAsync(30000));
  expect(mock.detail).toHaveBeenCalledTimes(calls);
});
