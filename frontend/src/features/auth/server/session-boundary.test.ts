// @vitest-environment node
import axios from "axios";
import { beforeEach, expect, it, vi } from "vitest";
import { GET as debts } from "@/app/api/debts/route";
import { GET as sessionProfile } from "@/app/api/auth/session/route";
import { GET as movements } from "@/app/api/customers/[id]/movements/route";
import { POST as create } from "@/app/api/charges/route";
import { readSession, deleteSession } from "./session";

vi.mock("server-only", () => ({}));
vi.mock("./session", () => ({ readSession: vi.fn(), deleteSession: vi.fn() }));
vi.mock("axios", () => ({ default: { request: vi.fn() } }));

beforeEach(() => {
  vi.resetAllMocks();
  vi.stubEnv("API_URL", "http://dotnet:5000");
  vi.stubEnv("APP_ORIGIN", undefined);
  vi.mocked(readSession).mockResolvedValue({
    token: "server-bearer",
    displayName: "Usuário",
    tenantId: "tenant-a",
    contextId: "session-a",
    expiresAt: "2099-01-01",
  });
  vi.mocked(axios.request).mockResolvedValue({
    status: 200,
    data: '{"id":"charge-a"}',
    headers: { "content-type": "application/json" },
  });
});

it("returns the validated session without a second API request or exposing the bearer", async () => {
  const response = await sessionProfile(sessionRequest("http://localhost:3000/api/auth/session"));
  expect(response.status).toBe(200);
  expect(await response.json()).toEqual({ authenticated: true, displayName: "Usuário", tenantId: "tenant-a",
    contextId: "session-a", expiresAt: "2099-01-01" });
  expect(readSession).toHaveBeenCalledTimes(1);
  expect(axios.request).not.toHaveBeenCalled();
  expect(response.headers.get("cache-control")).toBe("private, no-store");
});

it("keeps the cookie and reports session lookup outages as retryable failures", async () => {
  vi.mocked(readSession).mockRejectedValue(new Error("simulated infrastructure detail"));
  const response = await debts(sessionRequest("http://localhost:3000/api/debts"));
  expect(response.status).toBe(503);
  expect(await response.text()).not.toContain("simulated infrastructure detail");
  expect(deleteSession).not.toHaveBeenCalled();
  expect(axios.request).not.toHaveBeenCalled();
});

it("forwards only the financial payload and idempotency key with the server bearer", async () => {
  const response = await create(
    sessionRequest("http://localhost:3000/api/charges", {
      method: "POST",
      headers: {
        Origin: "http://localhost:3000",
        "Idempotency-Key": "stable-key",
        Authorization: "Bearer attacker",
        Cookie: "private",
        "X-Tenant-Id": "attacker",
      },
      body: '{"amount":42.25}',
    }),
  );
  expect(response.status).toBe(200);
  expect(await response.json()).toEqual({ id: "charge-a" });
  expect(response.headers.get("cache-control")).toBe("private, no-store");
  expect(axios.request).toHaveBeenCalledExactlyOnceWith(
    expect.objectContaining({
      baseURL: "http://dotnet:5000",
      url: "/api/charges",
      method: "POST",
      data: '{"amount":42.25}',
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer server-bearer",
        "Idempotency-Key": "stable-key",
      },
      maxRedirects: 0,
    }),
  );
});

it("rejects unauthenticated calls and cross-origin mutations before contacting the backend", async () => {
  vi.mocked(readSession).mockResolvedValue(null);
  expect((await debts(sessionRequest("http://localhost:3000/api/debts"))).status).toBe(401);
  expect(
    (
      await create(
        sessionRequest("http://localhost:3000/api/charges", {
          method: "POST",
          headers: { Origin: "https://attacker.example" },
        }),
      )
    ).status,
  ).toBe(401);
  expect(axios.request).not.toHaveBeenCalled();
});

it("accepts the browser host when Next binds to 0.0.0.0 and still rejects other sites", async () => {
  const request = (origin: string, site = "same-origin") =>
    sessionRequest("http://0.0.0.0:3000/api/charges", {
      method: "POST",
      headers: { Host: "localhost:3000", Origin: origin, "Sec-Fetch-Site": site },
      body: '{"amount":42.25}',
    });
  expect((await create(request("http://localhost:3000"))).status).toBe(200);
  expect(axios.request).toHaveBeenCalledOnce();
  expect((await create(request("https://attacker.example"))).status).toBe(403);
  expect((await create(request("http://localhost:3000", "cross-site"))).status).toBe(403);
  expect(axios.request).toHaveBeenCalledOnce();
});

it("accepts the current domain dynamically without a fixed public origin setting", async () => {
  const request = (origin: string) =>
    sessionRequest("https://0.0.0.0:3000/api/charges", {
      method: "POST",
      headers: { Host: "finance.example.com", Origin: origin },
      body: '{"amount":42.25}',
    });
  expect((await create(request("https://finance.example.com"))).status).toBe(200);
  expect((await create(request("http://internal:3000"))).status).toBe(403);
  expect(axios.request).toHaveBeenCalledOnce();
});

it.each(["http://localhost:3000", "http://192.168.15.3:3000", "https://apresentacao.example"])(
  "accepts same-origin operations at %s without any address registration", async (origin) => {
    const url = new URL(origin);
    const request = sessionRequest(`${url.protocol}//0.0.0.0/api/charges`, {
      method: "POST", headers: { Host: url.host, Origin: origin }, body: '{"amount":42.25}',
    });
    expect((await create(request)).status).toBe(200);
  },
);

it("rejects a stale tab context before using a new session's credentials for a financial POST", async () => {
  vi.mocked(readSession).mockResolvedValue({
    token: "new-session-bearer",
    displayName: "Outro usuário",
    tenantId: "tenant-a",
    contextId: "session-new",
    expiresAt: "2099-01-01",
  });
  const response = await create(
    sessionRequest("http://localhost:3000/api/charges", {
      method: "POST",
      headers: {
        Origin: "http://localhost:3000",
        "X-Yalla-Context": "session-old",
        "Idempotency-Key": "stable-key",
      },
      body: '{"amount":42.25}',
    }),
  );
  expect(response.status).toBe(409);
  expect(await response.json()).toMatchObject({ code: "SESSION_CONTEXT_CHANGED" });
  expect(axios.request).not.toHaveBeenCalled();
});

it("preserves query strings and encodes customer IDs without an arbitrary proxy destination", async () => {
  await debts(
    sessionRequest("http://localhost:3000/api/debts?page=2&status=Open&categories=a%2Cb"),
  );
  expect(axios.request).toHaveBeenLastCalledWith(
    expect.objectContaining({ url: "/api/debts?page=2&status=Open&categories=a%2Cb" }),
  );
  await movements(
    sessionRequest(
      "http://localhost:3000/api/customers/customer/movements?occurrenceDate=2026-09-01",
    ),
    { params: Promise.resolve({ id: "customer/a" }) },
  );
  expect(axios.request).toHaveBeenLastCalledWith(
    expect.objectContaining({
      url: "/api/customers/customer%2Fa/movements?occurrenceDate=2026-09-01",
    }),
  );
});

it("distinguishes backend 401/403 and removes expired sessions", async () => {
  vi.mocked(axios.request).mockResolvedValueOnce({
    status: 401,
    data: '{"code":"UNAUTHORIZED"}',
    headers: {},
  });
  expect((await debts(sessionRequest("http://localhost:3000/api/debts"))).status).toBe(401);
  expect(deleteSession).toHaveBeenCalledOnce();
  vi.mocked(axios.request).mockResolvedValueOnce({
    status: 403,
    data: '{"code":"FORBIDDEN"}',
    headers: {},
  });
  expect((await debts(sessionRequest("http://localhost:3000/api/debts"))).status).toBe(403);
  expect(deleteSession).toHaveBeenCalledOnce();
});

it("keeps concurrent credentials attached to their own requests and never retries a failed POST", async () => {
  vi.mocked(readSession)
    .mockResolvedValueOnce({
      token: "first",
      displayName: "A",
      contextId: "session-a",
      expiresAt: "2099-01-01",
    })
    .mockResolvedValueOnce({
      token: "second",
      displayName: "B",
      contextId: "session-a",
      expiresAt: "2099-01-01",
    });
  await Promise.all([
    debts(sessionRequest("http://localhost:3000/api/debts?page=1")),
    debts(sessionRequest("http://localhost:3000/api/debts?page=2")),
  ]);
  expect(
    vi.mocked(axios.request).mock.calls.map(([config]) => config?.headers?.Authorization),
  ).toEqual(["Bearer first", "Bearer second"]);
  vi.mocked(axios.request).mockRejectedValue(new Error("Connection lost"));
  const response = await create(
    sessionRequest("http://localhost:3000/api/charges", {
      method: "POST",
      headers: { Origin: "http://localhost:3000" },
    }),
  );
  expect(response.status).toBe(502);
  expect(axios.request).toHaveBeenCalledTimes(3);
});

function sessionRequest(url: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  if (!headers.has("X-Yalla-Context")) headers.set("X-Yalla-Context", "session-a");
  return new Request(url, { ...init, headers });
}
