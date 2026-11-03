import { beforeEach, expect, it, vi } from "vitest";
import { directoryProxy } from "./directory-proxy";
import { backendResponse } from "@/services/api/api-client.server";
vi.mock("server-only", () => ({}));
vi.mock("@/features/auth/server/session-boundary", () => ({
  authenticatedResponse: vi.fn(async (_request, operation) => operation("server-token", { token: "server-token", displayName: "Usuário", expiresAt: "2099-01-01" })),
}));
vi.mock("@/services/api/api-client.server", () => ({ backendResponse: vi.fn() }));
beforeEach(() => vi.resetAllMocks());
it("forwards user mutations through the authenticated boundary", async () => {
  // Restore the boundary behavior after resetAllMocks.
  const { authenticatedResponse } = await import("@/features/auth/server/session-boundary");
  vi.mocked(authenticatedResponse).mockImplementation(async (_request, operation) => operation("server-token", { token: "server-token", displayName: "Usuário", expiresAt: "2099-01-01" }));
  vi.mocked(backendResponse).mockResolvedValue(new Response(null, { status: 204 }));
  const id = "7f5f4d4e-5c6a-4c1a-9c50-7e2f3b8d1a10";
  const response = await directoryProxy(new Request("http://localhost/api/users/" + id + "/active", {
    method: "PUT", body: JSON.stringify({ active: false, confirmGlobalImpact: true }),
  }), "users", [id, "active"]);
  expect(response.status).toBe(204);
  expect(backendResponse).toHaveBeenCalledWith("/api/users/" + id + "/active", "server-token",
    expect.objectContaining({ method: "PUT", body: JSON.stringify({ active: false, confirmGlobalImpact: true }) }));
});
it("forwards the access-profile functionality catalog through the authenticated boundary", async () => {
  const { authenticatedResponse } = await import("@/features/auth/server/session-boundary");
  vi.mocked(authenticatedResponse).mockImplementation(async (_request, operation) => operation("server-token", { token: "server-token", displayName: "Usuário", expiresAt: "2099-01-01" }));
  vi.mocked(backendResponse).mockResolvedValue(Response.json([]));
  const response = await directoryProxy(new Request("http://localhost/api/access-profiles/functionalities"),
    "access-profiles", ["functionalities"]);
  expect(response.status).toBe(200);
  expect(backendResponse).toHaveBeenCalledWith("/api/access-profiles/functionalities", "server-token",
    expect.objectContaining({ method: "GET" }));
});
it("refuses unrelated paths before contacting the backend", async () => {
  const response = await directoryProxy(new Request("http://localhost/api/users/other"), "users", ["..", "payments"]);
  expect(response.status).toBe(404); expect(backendResponse).not.toHaveBeenCalled();
});
it("refuses writes to the initial password setting before contacting the backend", async () => {
  const response = await directoryProxy(new Request("http://localhost/api/users/initial-password", {
    method: "PUT", body: JSON.stringify({ password: "unused-password" }),
  }), "users", ["initial-password"]);
  expect(response.status).toBe(405);
  expect(response.headers.get("Allow")).toBe("GET");
  expect(backendResponse).not.toHaveBeenCalled();
});
it.each([
  ["linkable-tenants"],
  ["7f5f4d4e-5c6a-4c1a-9c50-7e2f3b8d1a10", "access"],
  ["linkable-tenants", "7f5f4d4e-5c6a-4c1a-9c50-7e2f3b8d1a10", "profiles"],
  ["7f5f4d4e-5c6a-4c1a-9c50-7e2f3b8d1a10", "tenants", "3c2d1e0f-9a8b-47c6-b5d4-2f1e0a9b8c7d"],
])("refuses removed tenant-link routes: %s", async (...path) => {
  const response = await directoryProxy(new Request(`http://localhost/api/users/${path.join("/")}`), "users", path);
  expect(response.status).toBe(404);
  expect(backendResponse).not.toHaveBeenCalled();
});

