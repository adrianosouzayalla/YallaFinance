import { createElement, type PropsWithChildren } from "react";
import { act, cleanup, renderHook } from "@testing-library/react";
import { QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SessionProvider } from "@/features/auth/context/SessionProvider";
import { createQueryClient } from "@/providers/QueryProvider";
import { chargesApi } from "@/features/charges/api/charges.client";
import { chargesServerApi } from "@/features/charges/api/charges.server";
import { useSubmitDirectCharge } from "@/features/charges/hooks/useCreateCharge";
import { POST } from "@/app/api/charges/direct/route";
import { GET as refreshStatus } from "@/app/api/charges/direct/[id]/status/route";
import { buildDirectChargeRequest } from "./directChargeDraft";
import {
  createDirectChargeFixture,
  directChargeReceipt,
  directChargeResponse,
} from "@/test/directChargeFixtures";

const mocks = vi.hoisted(() => ({ request: vi.fn(), backendResponse: vi.fn(), boundary: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/services/api/api-client", () => ({ request: mocks.request }));
vi.mock("@/services/api/api-client.server", () => ({ backendResponse: mocks.backendResponse }));
vi.mock("@/features/auth/server/session-boundary", () => ({
  authenticatedResponse: mocks.boundary,
}));
beforeEach(() => {
  vi.resetAllMocks();
  mocks.request.mockResolvedValue(directChargeResponse);
  mocks.backendResponse.mockResolvedValue(Response.json(directChargeResponse));
  mocks.boundary.mockImplementation(
    (_request: Request, action: (token: string) => Promise<Response>) =>
      action("unit-server-token"),
  );
  vi.stubGlobal(
    "fetch",
    vi.fn(() => {
      throw new Error("Unexpected network");
    }),
  );
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function payload() {
  const example = createDirectChargeFixture();
  return buildDirectChargeRequest(example.basics, example.debts);
}

describe("Direct charge reception transport", () => {
  it("reads simulated status through the authenticated GET without creating another charge", async () => {
    const signal = new AbortController().signal;
    const id = directChargeReceipt.id;
    expect(await chargesApi.refreshDirectChargeStatus(id, { contextId: "ctx", signal })).toEqual(
      directChargeReceipt,
    );
    expect(mocks.request).toHaveBeenCalledExactlyOnceWith(`/api/charges/direct/${id}/status`, {
      headers: { "X-Yalla-Context": "ctx" },
      signal,
    });
    const request = new Request(`http://unit.invalid/api/charges/direct/${id}/status`);
    expect(
      await (await refreshStatus(request, { params: Promise.resolve({ id }) })).json(),
    ).toEqual(directChargeResponse);
    expect(mocks.boundary).toHaveBeenCalledWith(request, expect.any(Function));
    expect(mocks.backendResponse).toHaveBeenCalledExactlyOnceWith(
      `/api/charges/direct/${id}/status`,
      "unit-server-token",
      { signal: request.signal },
    );
  });

  it("rejects invalid status and responses from another charge", async () => {
    mocks.request.mockResolvedValueOnce({
      ...directChargeResponse,
      id: "55555555-5555-4555-8555-555555555555",
    });
    await expect(
      chargesApi.refreshDirectChargeStatus(directChargeReceipt.id, { contextId: "ctx" }),
    ).rejects.toMatchObject({ code: "INVALID_DIRECT_CHARGE_RECEIPT" });
    mocks.request.mockResolvedValueOnce({ received: true });
    await expect(
      chargesApi.refreshDirectChargeStatus(directChargeReceipt.id, { contextId: "ctx" }),
    ).rejects.toMatchObject({ code: "INVALID_DIRECT_CHARGE_RECEIPT" });
  });
  it("builds every item with exact cents and no operational identifiers", () => {
    const example = createDirectChargeFixture();
    const debts = Array.from({ length: 5 }, (_, index) => ({
      ...example.debts[index % 2],
      id: index + 1,
      amount: "9007199254740993.01",
    }));
    const body = buildDirectChargeRequest(
      { ...example.basics, document: "012.345.678-90", contract: " CT-7 ", plate: "abc1d23" },
      debts,
    );
    expect(body.customerDocument).toBe("01234567890");
    expect(body.contract).toBe("CT-7");
    expect(body.plate).toBe("ABC1D23");
    expect(body.items).toHaveLength(5);
    expect(body.items[0]).toEqual({
      categoryId: example.debts[0].categoryId,
      categoryName: "Multa do banco",
      occurrenceDate: "2030-10-20",
      amount: "9007199254740993.01",
      descriptionDetail: "Infração informada",
    });
    expect(body.customerNote).toBe("");
    expect(Object.keys(body).sort()).toEqual(
      [
        "customerDocument",
        "customerName",
        "customerDocumentType",
        "customerEmail",
        "customerPhone",
        "contract",
        "plate",
        "costCenterName",
        "costCenterReference",
        "dueDate",
        "customerNote",
        "items",
      ].sort(),
    );
  });

  it("posts the receipt command with context, attempt key and cancellation without mapping to Charge", async () => {
    const body = payload();
    const signal = new AbortController().signal;
    expect(
      await chargesApi.submitDirectCharge(body, "attempt-key", {
        contextId: "session-unit",
        signal,
      }),
    ).toEqual(directChargeReceipt);
    expect(mocks.request).toHaveBeenCalledExactlyOnceWith("/api/charges/direct", {
      method: "POST",
      headers: { "X-Yalla-Context": "session-unit", "Idempotency-Key": "attempt-key" },
      signal,
      body: JSON.stringify(body),
    });
  });

  it.each([
    undefined,
    null,
    {},
    { received: false, processed: false },
    { received: true, processed: true },
    { received: "true", processed: false },
    { ...directChargeResponse, id: "invalid" },
    { ...directChargeResponse, id: "00000000-0000-0000-0000-000000000000" },
    { ...directChargeResponse, reference: " " },
    { ...directChargeResponse, source: "Standalone" },
  ])("rejects an invalid HTTP success body: %j", async (receipt) => {
    mocks.request.mockResolvedValue(receipt);
    await expect(
      chargesApi.submitDirectCharge(payload(), "key", { contextId: "session" }),
    ).rejects.toMatchObject({ code: "INVALID_DIRECT_CHARGE_RECEIPT" });
  });

  it("forwards the authenticated Next request unchanged to the receiving endpoint", async () => {
    const body = JSON.stringify(payload());
    const request = new Request("http://unit.invalid/api/charges/direct", {
      method: "POST",
      body,
      headers: { "Idempotency-Key": "unit-key" },
    });
    const response = await POST(request);
    expect(await response.json()).toEqual(directChargeResponse);
    expect(mocks.boundary).toHaveBeenCalledWith(request, expect.any(Function));
    expect(mocks.backendResponse).toHaveBeenCalledExactlyOnceWith(
      "/api/charges/direct",
      "unit-server-token",
      {
        method: "POST",
        body,
        headers: { "Idempotency-Key": "unit-key" },
        signal: request.signal,
      },
    );
  });

  it("preserves upstream failures and does not forward when the session boundary rejects", async () => {
    const response = Response.json(
      { errors: [{ code: "ServiceUnavailable", message: "Indisponível" }] },
      { status: 503 },
    );
    mocks.backendResponse.mockResolvedValue(response);
    expect(await chargesServerApi.submitDirectCharge("{}", null, "unit-token")).toBe(response);
    mocks.backendResponse.mockClear();
    mocks.boundary.mockResolvedValue(new Response(null, { status: 403 }));
    expect((await POST(new Request("http://unit.invalid", { method: "POST" }))).status).toBe(403);
    expect(mocks.backendResponse).not.toHaveBeenCalled();
  });

  it("preserves the created charge and refreshes debt caches without retrying failures", async () => {
    const client = createQueryClient();
    const invalidate = vi.spyOn(client, "invalidateQueries");
    function Wrapper({ children }: PropsWithChildren) {
      return createElement(
        QueryClientProvider,
        { client },
        createElement(
          SessionProvider,
          {
            session: { tenantId: "tenant-unit", contextId: "context-unit" },
          },
          children,
        ),
      );
    }
    const hook = renderHook(() => useSubmitDirectCharge(), { wrapper: Wrapper });
    await act(async () => {
      await hook.result.current.mutateAsync({ payload: payload(), idempotencyKey: "key" });
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ["debts", "tenant-unit", "context-unit"] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ["dashboard", "tenant-unit", "context-unit"] });
    expect(client.getQueryCache().getAll()).toHaveLength(1);
    mocks.request.mockRejectedValue(new Error("transport unavailable"));
    await act(async () => {
      await expect(
        hook.result.current.mutateAsync({ payload: payload(), idempotencyKey: "key" }),
      ).rejects.toThrow("transport unavailable");
    });
    expect(mocks.request).toHaveBeenCalledTimes(2);
    expect(invalidate).toHaveBeenCalledTimes(2);
  });
});
