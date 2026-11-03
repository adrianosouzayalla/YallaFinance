import { expect, it, vi } from "vitest";
import { jobsApi } from "./jobs.client";
const mock = vi.hoisted(() => ({ request: vi.fn().mockResolvedValue({ items: [] }) }));
vi.mock("@/services/api/api-client", () => ({ request: mock.request }));
it("sends local day boundaries including the entire final day", async () => {
  mock.request.mockClear();
  await jobsApi.list({ key: "", type: "", status: "", page: 1, pageSize: 20, startDate: "2026-10-15", endDate: "2026-10-15" }, { contextId: "session" });
  const params = new URL(mock.request.mock.lastCall![0], "http://localhost").searchParams;
  expect(new Date(params.get("startDate")!).getTime()).toBe(new Date(2026, 9, 15).getTime());
  expect(new Date(params.get("endDate")!).getTime()).toBe(new Date(2026, 9, 16).getTime());
});
it("encodes literal key filters and keeps context and cancellation", async () => {
  const signal = new AbortController().signal;
  await jobsApi.list(
    { key: "charge:%_\\", status: "Processing", type: "TYPE", page: 2, pageSize: 20 },
    { contextId: "session", signal },
  );
  const [url, options] = mock.request.mock.calls[0];
  const params = new URL(url, "http://localhost").searchParams;
  expect(params.get("key")).toBe("charge:%_\\");
  expect(params.get("status")).toBe("Processing");
  expect(params.get("page")).toBe("2");
  expect(options.headers["X-Yalla-Context"]).toBe("session");
  expect(options.signal).toBe(signal);
});
it("reprocesses by authenticated POST without payload or browser credentials", async () => {
  mock.request.mockClear();
  await jobsApi.reprocess("job/id", { contextId: "session" });
  const [url, options] = mock.request.mock.calls[0];
  expect(url).toBe("/api/jobs/job%2Fid/reprocess");
  expect(options.method).toBe("POST");
  expect(options.headers["X-Yalla-Context"]).toBe("session");
  expect(options.body).toBeUndefined();
  expect(options.headers.Authorization).toBeUndefined();
});
