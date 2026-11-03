import { beforeEach, expect, it, vi } from "vitest";
import { POST } from "./route";
import { deleteSession } from "@/features/auth/server/session";
import { backendResponse } from "@/services/api/api-client.server";
vi.mock("server-only", () => ({}));
vi.mock("@/features/auth/server/session-boundary", () => ({ authenticatedResponse: vi.fn(async (_request, operation) => operation("server-token")) }));
vi.mock("@/features/auth/server/session", () => ({ deleteSession: vi.fn() }));
vi.mock("@/services/api/api-client.server", () => ({ backendResponse: vi.fn() }));
beforeEach(() => { vi.clearAllMocks(); });
it("only deletes the browser cookie after password change commits", async () => {
  vi.mocked(backendResponse).mockResolvedValueOnce(new Response(null, { status: 503 }));
  await POST(new Request("http://localhost/api/auth/password", { method: "POST", body: "{}" }));
  expect(deleteSession).not.toHaveBeenCalled();
  vi.mocked(backendResponse).mockResolvedValueOnce(new Response(null, { status: 204 }));
  await POST(new Request("http://localhost/api/auth/password", { method: "POST", body: "{}" }));
  expect(deleteSession).toHaveBeenCalledTimes(1);
});

