import { beforeEach, expect, it, vi } from "vitest";
import { debtsApi } from "./debts.client";
const mocks = vi.hoisted(() => ({ request: vi.fn() }));
vi.mock("@/services/api/api-client", () => ({ request: mocks.request, ApiError: class extends Error {} }));
beforeEach(() => vi.clearAllMocks());

it("fetches selected debt details and categories once without listing the catalog", async () => {
  mocks.request.mockImplementation(async (path: string) => path === "/api/categories"
    ? [{ id: "fine", name: "Multa" }]
    : { id: path.split("/").at(-1), categoryId: "fine", chargeId: null });
  const result = await debtsApi.getDebts(["debt-100001", "debt-2", "debt-2"], { contextId: "session" });
  expect(result.map((debt) => debt.id)).toEqual(["debt-100001", "debt-2"]);
  expect(result[0].category).toBe("Multa");
  expect(mocks.request.mock.calls.map(([path]) => path)).toEqual([
    "/api/categories", "/api/debts/debt-100001", "/api/debts/debt-2",
  ]);
  expect(mocks.request.mock.calls.every(([, options]) => options.headers["X-Yalla-Context"] === "session")).toBe(true);
});

it("propagates missing or unauthorized selected debts rather than returning a partial selection", async () => {
  mocks.request.mockImplementation(async (path: string) => {
    if (path === "/api/categories") return [];
    throw new Error("Debt unavailable");
  });
  await expect(debtsApi.getDebts(["missing"], { contextId: "session" })).rejects.toThrow("Debt unavailable");
});

it("does not request anything for an empty selection", async () => {
  expect(await debtsApi.getDebts([], { contextId: "session" })).toEqual([]);
  expect(mocks.request).not.toHaveBeenCalled();
});
