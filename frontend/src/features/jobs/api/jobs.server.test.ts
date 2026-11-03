import { expect, it, vi } from "vitest";
import { jobsServerApi } from "./jobs.server";
const mock = vi.hoisted(() => ({ backendResponse: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/services/api/api-client.server", () => mock);

it("keeps the real notification conflict message compatible with the browser API client", async () => {
  const message = "O estado deste Job mudou. Atualize os dados antes de tentar novamente.";
  mock.backendResponse.mockResolvedValue(
    Response.json({ errors: [{ code: "Conflict", message }] }, { status: 409 }),
  );
  const signal = new AbortController().signal;
  const response = await jobsServerApi.reprocess("job", "server-token", signal);
  expect(response.status).toBe(409);
  expect(await response.json()).toEqual({ code: "Conflict", detail: message });
  expect(response.headers.get("Cache-Control")).toBe("private, no-store");
  expect(mock.backendResponse).toHaveBeenLastCalledWith("/api/jobs/job/reprocess", "server-token", {
    method: "POST",
    signal,
  });
});
it("does not modify success or turn malformed conflicts into success", async () => {
  const success = Response.json({ message: "Job enviado para reprocessamento." });
  mock.backendResponse.mockResolvedValue(success);
  expect(await jobsServerApi.reprocess("job", "token")).toBe(success);
  const invalid = new Response("invalid", { status: 409 });
  mock.backendResponse.mockResolvedValue(invalid);
  const response = await jobsServerApi.reprocess("job", "token");
  expect(response.status).toBe(409);
  expect(await response.text()).toBe("invalid");
});
