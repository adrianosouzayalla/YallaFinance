import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import type { PropsWithChildren } from "react";
import { SessionProvider } from "@/features/auth/context/SessionProvider";
import { createQueryClient } from "@/providers/QueryProvider";
import { useCreateCharge } from "./useCreateCharge";
import { useCharge } from "./useCharge";
import type { Charge, ChargeDraft } from "../types/charge.types";

const mocks = vi.hoisted(() => ({ create: vi.fn(), get: vi.fn() }));
vi.mock("../api/charges.client", () => ({
  chargesApi: { createCharge: mocks.create, getCharge: mocks.get },
}));
const charge: Charge = {
  reference: "10-123",
  id: "charge-a",
  tenantId: "tenant-test",
  customerId: "customer-a",
  source: "selected",
  idempotencyKey: "stable",
  categoryId: "fine",
  paymentMethodId: "pix",
  debtIds: ["debt-a"],
  amount: 42.25,
  payment: { method: "Pix", dueDate: "2026-10-05", customerNote: "" },
  providerExternalId: "receivable-a",
  pixId: "pix-a",
  pixPayload: "payload-from-post",
  createdAt: "2026-10-01T12:00:00Z",
};
const draft: ChargeDraft = {
  source: "selected",
  customerId: "customer-a",
  customerName: "Cliente",
  debtIds: ["debt-a"],
  amount: 42.25,
  payment: charge.payment,
};
beforeEach(() => {
  vi.resetAllMocks();
  mocks.create.mockResolvedValue(charge);
  mocks.get.mockResolvedValue(charge);
});
afterEach(cleanup);

it("uses the POST result for immediate success without requiring another request", async () => {
  const client = createQueryClient();
  function Wrapper({ children }: PropsWithChildren) {
    return (
      <QueryClientProvider client={client}>
        <SessionProvider session={{ tenantId: "tenant-test", contextId: "session-test" }}>
          {children}
        </SessionProvider>
      </QueryClientProvider>
    );
  }
  const creation = renderHook(() => useCreateCharge(), { wrapper: Wrapper });
  await act(async () => {
    await creation.result.current.mutateAsync({ draft, idempotencyKey: "stable" });
  });
  const success = renderHook(() => useCharge("charge-a"), { wrapper: Wrapper });
  expect(success.result.current.data).toMatchObject({
    id: "charge-a",
    pixPayload: "payload-from-post",
  });
  expect(mocks.get).not.toHaveBeenCalled();
});

it("queries the backend when a new browser cache has no creation response", async () => {
  const client = createQueryClient();
  function Wrapper({ children }: PropsWithChildren) {
    return (
      <QueryClientProvider client={client}>
        <SessionProvider session={{ tenantId: "tenant-test", contextId: "new-session" }}>
          {children}
        </SessionProvider>
      </QueryClientProvider>
    );
  }
  const success = renderHook(() => useCharge("charge-a"), { wrapper: Wrapper });
  await waitFor(() => expect(success.result.current.data?.id).toBe("charge-a"));
  expect(mocks.get).toHaveBeenCalledExactlyOnceWith(
    "charge-a",
    expect.objectContaining({ contextId: "new-session", signal: expect.any(AbortSignal) }),
  );
  expect(mocks.create).not.toHaveBeenCalled();
});
