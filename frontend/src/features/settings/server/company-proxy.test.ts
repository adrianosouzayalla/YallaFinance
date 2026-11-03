import { beforeEach, expect, it, vi } from "vitest";
import { companyProxy } from "./company-proxy";
import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { backendResponse } from "@/services/api/api-client.server";
vi.mock("server-only", () => ({}));
vi.mock("@/features/auth/server/session-boundary", () => ({ authenticatedResponse: vi.fn() }));
vi.mock("@/services/api/api-client.server", () => ({ backendResponse: vi.fn() }));
beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(authenticatedResponse).mockImplementation(async (_request, operation) =>
    operation("token", {} as never),
  );
  vi.mocked(backendResponse).mockResolvedValue(Response.json({}));
});
it("allows the Asaas provider filter through the existing connection resource", async () => {
  const response = await companyProxy(new Request("http://localhost/api/company/connections?provider=Asaas"), ["connections"]);
  expect(response.status).toBe(200);
  expect(backendResponse).toHaveBeenCalledWith("/api/company/connections?provider=Asaas", "token", expect.anything());
});

it.each(["CreditCard", "Pix", "Boleto", null])("forwards administrative instrument %s and optional description", async (instrumentType) => {
  const id = "11111111-1111-1111-1111-111111111111";
  const payload = { paymentMethodId: id, enabled: false, instrumentType, internalDescription: "Conta interna" };
  const request = new Request(`http://localhost/api/company/connections/${id}/payment-methods`, {
    method: "POST", headers: { "content-type": "application/json", origin: "http://localhost" }, body: JSON.stringify(payload),
  });
  expect((await companyProxy(request, ["connections", id, "payment-methods"])).status).toBe(200);
  expect(backendResponse).toHaveBeenCalledWith(`/api/company/connections/${id}/payment-methods`, "token", {
    method: "POST", body: JSON.stringify(payload), signal: request.signal,
  });
});

it.each(["DebitCard", "", "creditCard", 1, {}, []])("rejects an unsupported instrument value %s", async (instrumentType) => {
  const id = "11111111-1111-1111-1111-111111111111";
  const request = new Request(`http://localhost/api/company/connections/${id}/payment-methods`, {
    method: "POST", headers: { "content-type": "application/json", origin: "http://localhost" },
    body: JSON.stringify({ paymentMethodId: id, enabled: true, instrumentType }),
  });
  expect((await companyProxy(request, ["connections", id, "payment-methods"])).status).toBe(400);
  expect(backendResponse).not.toHaveBeenCalled();
});
it.each(["payments", "session", "user-registration", "financial-settlement"])(
  "forwards settings/%s with cancellation",
  async (group) => {
    const request = new Request(`http://localhost/api/company/settings/${group}`);
    const response = await companyProxy(request, ["settings", group]);
    expect(response.headers.get("Cache-Control")).toBe("private, no-store");
    expect(backendResponse).toHaveBeenCalledWith(`/api/company/settings/${group}`, "token", {
      method: "GET",
      signal: request.signal,
    });
  },
);
it.each([
  "tenantId=other",
  "page=0",
  "pageSize=101",
  "enabled=1",
  "sortBy=secret",
  "page=1&page=2",
  "provider=Other",
])("rejects invalid list filters %s", async (query) => {
  expect(
    (
      await companyProxy(new Request(`http://localhost/api/company/connections?${query}`), [
        "connections",
      ])
    ).status,
  ).toBe(400);
  expect(backendResponse).not.toHaveBeenCalled();
});
it.each([
  ["settings", "unknown"],
  ["connections", "not-guid"],
  ["connections", "11111111-1111-1111-1111-111111111111", "mappings"],
])("rejects unrelated paths %s", async (...path) => {
  expect((await companyProxy(new Request("http://localhost/api/company"), path)).status).toBe(404);
});
it("rejects writes to read-only connection collections and respects missing session context", async () => {
  expect(
    (
      await companyProxy(
        new Request("http://localhost/api/company/connections", { method: "POST" }),
        ["connections", "11111111-1111-1111-1111-111111111111", "mappings"],
      )
    ).status,
  ).toBe(404);
  expect((await companyProxy(new Request("http://localhost/api/company/connections", { method: "DELETE" }), ["connections"])).status).toBe(405);
  vi.mocked(authenticatedResponse).mockResolvedValue(Response.json({}, { status: 409 }));
  expect(
    (await companyProxy(new Request("http://localhost/api/company/connections"), ["connections"]))
      .status,
  ).toBe(409);
  expect(backendResponse).not.toHaveBeenCalled();
});

it("forwards an allowlisted connection write and its body through the authenticated boundary", async () => {
  const id = "11111111-1111-1111-1111-111111111111";
  const payload = { name: "Sicoob principal", provider: "Sicoob", group: "PaymentProvider", enabled: false };
  const request = new Request(`http://localhost/api/company/connections/${id}`, {
    method: "PUT", headers: { "content-type": "application/json", origin: "http://localhost" }, body: JSON.stringify(payload),
  });

  const response = await companyProxy(request, ["connections", id]);

  expect(response.status).toBe(200);
  expect(backendResponse).toHaveBeenCalledWith(`/api/company/connections/${id}`, "token", {
    method: "PUT", body: JSON.stringify(payload), signal: request.signal,
  });
});

it("does not forward invalid write payloads", async () => {
  const response = await companyProxy(new Request("http://localhost/api/company/settings/payments", {
    method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify({ maximumLinkDays: "30", publicOrigin: "", tenantId: "other" }),
  }), ["settings", "payments"]);
  expect(response.status).toBe(400);
  expect(backendResponse).not.toHaveBeenCalled();
});
it("rejects query filters on writes", async () => {
  const response = await companyProxy(new Request("http://localhost/api/company/connections?search=unrelated", {
    method: "POST", headers: { "content-type": "application/json" },
    body: JSON.stringify({ name: "Conexão", provider: "Nibo", group: "FinancialSystem", enabled: false }),
  }), ["connections"]);
  expect(response.status).toBe(400);
  expect(backendResponse).not.toHaveBeenCalled();
});
it("forwards validated independent detail pagination and sorting", async () => {
  const id = "11111111-1111-1111-1111-111111111111";
  const query = "categoryPage=2&receiptAccountSortBy=externalValue&paymentMethodSortDirection=desc";
  await companyProxy(new Request(`http://localhost/api/company/connections/${id}?${query}`), [
    "connections",
    id,
  ]);
  expect(backendResponse).toHaveBeenCalledWith(
    `/api/company/connections/${id}?${query}`,
    "token",
    expect.anything(),
  );
});
