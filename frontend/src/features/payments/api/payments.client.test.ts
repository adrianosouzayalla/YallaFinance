import { beforeEach, expect, it, vi } from "vitest";
import { request } from "@/services/api/api-client";
import { paymentsApi } from "./payments.client";

vi.mock("@/services/api/api-client", () => ({ request: vi.fn() }));
beforeEach(() => vi.clearAllMocks());

it("forwards an exact reference with date, status, ordering and pagination without number conversion", async () => {
  await paymentsApi.list({ reference: "10-9223372036854775807", startDate: "2026-10-01", endDate: "2026-10-17",
    paymentStatus: "available", sortBy: "charge", sortDirection: "asc", page: 2, pageSize: 5 }, { contextId: "session-test" });
  const url = new URL(vi.mocked(request).mock.calls[0][0], "http://unit.invalid");
  expect(Object.fromEntries(url.searchParams)).toEqual({ reference: "10-9223372036854775807", startDate: "2026-10-01",
    endDate: "2026-10-17", paymentStatus: "available", sortBy: "charge", sortDirection: "asc", page: "2", pageSize: "5" });
  expect(url.searchParams.has("tenantId")).toBe(false);
});

it("omits the reference when the filter is cleared", async () => {
  await paymentsApi.list({ startDate: "", endDate: "", page: 1, pageSize: 5 }, { contextId: "session-test" });
  expect(vi.mocked(request).mock.calls[0][0]).not.toContain("reference=");
});
