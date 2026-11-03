import { beforeEach, expect, it, vi } from "vitest";
import { financialCatalogApi } from "./financial-catalog.client";
import { catalogsServerApi } from "./catalogs.server";
import { GET, POST } from "@/app/api/financial-catalog/[...path]/route";
import { directCustomers, directCostCenters } from "@/test/directChargeFixtures";

const mocks = vi.hoisted(() => ({ request: vi.fn(), backend: vi.fn(), boundary: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/services/api/api-client", () => ({ request: mocks.request }));
vi.mock("@/services/api/api-client.server", () => ({ backendResponse: mocks.backend }));
vi.mock("@/features/auth/server/session-boundary", () => ({
  authenticatedResponse: mocks.boundary,
}));
beforeEach(() => {
  vi.resetAllMocks();
  mocks.boundary.mockImplementation(
    (_request: Request, callback: (token: string) => Promise<Response>) => callback("unit-token"),
  );
  mocks.backend.mockResolvedValue(Response.json(directCostCenters));
});

it("looks up normalized CPF/CNPJ through the generic authenticated financial endpoint", async () => {
  mocks.request.mockResolvedValue(directCustomers);
  const signal = new AbortController().signal;
  expect(
    await financialCatalogApi.findCustomersByDocument("123.456.789-01", {
      contextId: "ctx",
      signal,
    }),
  ).toEqual(directCustomers);
  expect(mocks.request).toHaveBeenCalledExactlyOnceWith("/api/financial-catalog/customers/lookup", {
    method: "POST",
    body: JSON.stringify({ document: "12345678901" }),
    headers: { "X-Yalla-Context": "ctx" },
    signal,
  });
});

it("loads every cost-center page without dropping items or changing financial connection", async () => {
  mocks.request
    .mockResolvedValueOnce({ ...directCostCenters, totalCount: 3 })
    .mockResolvedValueOnce({
      ...directCostCenters,
      totalCount: 3,
      items: [{ externalReference: "center-c", name: "Outro centro" }],
    });
  const result = await financialCatalogApi.listCostCenters({ contextId: "ctx" });
  expect(result.items).toHaveLength(3);
  expect(mocks.request.mock.calls[1][0]).toBe(
    "/api/financial-catalog/cost-centers?skip=2&take=500",
  );
});

it.each(["empty-page", "changed-connection", "repeated-reference"])(
  "rejects inconsistent financial pagination: %s",
  async (failure) => {
    mocks.request.mockResolvedValueOnce({ ...directCostCenters, totalCount: 3 });
    mocks.request.mockResolvedValueOnce({
      ...directCostCenters,
      totalCount: 3,
      connectionId: failure === "changed-connection" ? "different" : directCostCenters.connectionId,
      items: failure === "empty-page" ? [] : [{ externalReference: "center-a", name: "Repetido" }],
    });
    await expect(financialCatalogApi.listCostCenters({ contextId: "ctx" })).rejects.toMatchObject({
      code: "INVALID_FINANCIAL_CATALOG",
    });
  },
);

it("forwards only supported authenticated lookup and cost-center operations", async () => {
  const request = new Request("http://unit.invalid/api/financial-catalog/customers/lookup", {
    method: "POST",
    body: '{"document":"12345678901"}',
  });
  await POST(request, { params: Promise.resolve({ path: ["customers", "lookup"] }) });
  expect(mocks.backend).toHaveBeenLastCalledWith(
    "/api/financial-catalog/customers/lookup",
    "unit-token",
    { method: "POST", body: '{"document":"12345678901"}', signal: request.signal },
  );
  const get = new Request("http://unit.invalid/api/financial-catalog/cost-centers?skip=2&take=100");
  await GET(get, { params: Promise.resolve({ path: ["cost-centers"] }) });
  expect(mocks.backend).toHaveBeenLastCalledWith(
    "/api/financial-catalog/cost-centers?skip=2&take=100",
    "unit-token",
    { signal: get.signal },
  );
  mocks.backend.mockClear();
  expect((await GET(get, { params: Promise.resolve({ path: ["secrets"] }) })).status).toBe(404);
  expect(mocks.backend).not.toHaveBeenCalled();
  expect(catalogsServerApi).toHaveProperty("financialCustomers");
});
