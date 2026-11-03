import axios from "axios";
import { beforeEach, expect, it, vi } from "vitest";
import { apiClient, ApiError, request } from "./api-client";

vi.mock("axios", () => ({ default: {
  request: vi.fn(),
  isAxiosError: (error: unknown) => Boolean(error && typeof error === "object" && "isAxiosError" in error),
  isCancel: (error: unknown) => Boolean(error && typeof error === "object" && "code" in error && error.code === "ERR_CANCELED"),
} }));
beforeEach(() => {
  vi.resetAllMocks();
  document.cookie = "yalla-finance-session=; Max-Age=0; path=/";
});

it("returns the response body and forwards parameters, cancellation and custom headers", async () => {
  vi.mocked(axios.request).mockResolvedValue({ status: 200, data: '{"id":"a"}' });
  const controller = new AbortController();
  expect(
    await apiClient.get("/api/debts", {
      params: { page: 2, category: "A&B" },
      signal: controller.signal,
    }),
  ).toEqual({ id: "a" });
  expect(axios.request).toHaveBeenCalledWith(
    expect.objectContaining({ url: "/api/debts?page=2&category=A%26B", signal: controller.signal }),
  );
  await apiClient.post("/api/charges", { amount: 10 }, { headers: { "Idempotency-Key": "key" } });
  expect(axios.request).toHaveBeenLastCalledWith(
    expect.objectContaining({
      method: "POST",
      data: '{"amount":10}',
      headers: expect.objectContaining({ "idempotency-key": "key" }),
    }),
  );
});

it("returns successful plain text responses without parsing them as JSON", async () => {
  vi.mocked(axios.request).mockResolvedValue({
    status: 200,
    data: "Authentication",
    headers: { "content-type": "text/plain; charset=utf-8" },
  });

  await expect(request<string>("/api/company/settings/session", { method: "PUT", body: "{}" }))
    .resolves.toBe("Authentication");
});

it("handles empty responses and normalizes problem details without retry", async () => {
  vi.mocked(axios.request).mockResolvedValueOnce({ status: 204, data: "" });
  expect(await apiClient.post("/api/example")).toBeUndefined();
  vi.mocked(axios.request).mockResolvedValueOnce({
    status: 409,
    data: '{"detail":"Conflict","extensions":{"code":"DUPLICATE"}}',
  });
  await expect(apiClient.post("/api/charges", {})).rejects.toMatchObject({
    code: "DUPLICATE",
    status: 409,
    message: "Conflict",
  });
  expect(axios.request).toHaveBeenCalledTimes(2);
});

it("normalizes the API notification error collection", async () => {
  vi.mocked(axios.request).mockResolvedValue({
    status: 503,
    data: '{"errors":[{"code":"ServiceUnavailable","message":"A forma de pagamento está indisponível no momento."}]}',
  });

  await expect(apiClient.post("/api/payment/token/attempts", {})).rejects.toMatchObject({
    code: "ServiceUnavailable",
    status: 503,
    message: "A forma de pagamento está indisponível no momento.",
  });
});

it("normalizes a non-JSON failure", async () => {
  vi.mocked(axios.request).mockResolvedValue({ status: 502, data: "Bad gateway" });
  await expect(apiClient.get("/api/debts")).rejects.toBeInstanceOf(ApiError);
});

it("signals a session context change while keeping financial conflicts as local errors", async () => {
  const failure = vi.fn();
  window.addEventListener("yalla:auth-failure", failure);
  try {
    vi.mocked(axios.request).mockResolvedValueOnce({
      status: 409,
      data: '{"code":"SESSION_CONTEXT_CHANGED"}',
    });
    await expect(apiClient.post("/api/charges", {})).rejects.toMatchObject({
      code: "SESSION_CONTEXT_CHANGED",
    });
    expect(failure).toHaveBeenCalledOnce();
    vi.mocked(axios.request).mockResolvedValueOnce({ status: 409, data: '{"code":"DUPLICATE"}' });
    await expect(apiClient.post("/api/charges", {})).rejects.toMatchObject({ code: "DUPLICATE" });
    expect(failure).toHaveBeenCalledOnce();
  } finally {
    window.removeEventListener("yalla:auth-failure", failure);
  }
});

it("does not treat a functionality permission 403 as an expired session", async () => {
  const failure = vi.fn();
  window.addEventListener("yalla:auth-failure", failure);
  try {
    vi.mocked(axios.request).mockResolvedValue({ status: 403, data: '{"code":"FORBIDDEN"}' });
    await expect(apiClient.get("/api/profiles")).rejects.toMatchObject({ status: 403 });
    expect(failure).not.toHaveBeenCalled();
  } finally {
    window.removeEventListener("yalla:auth-failure", failure);
  }
});

it("signals only explicit API unavailability and leaves ordinary HTTP errors local", async () => {
  const unavailable = vi.fn();
  window.addEventListener("yalla:api-unavailable", unavailable);
  try {
    vi.mocked(axios.request).mockResolvedValueOnce({ status: 503, data: '{"code":"API_UNAVAILABLE"}' });
    await expect(apiClient.get("/api/debts")).rejects.toMatchObject({ status: 503 });
    expect(unavailable).toHaveBeenCalledOnce();

    vi.mocked(axios.request).mockResolvedValueOnce({ status: 503, data: '{"code":"ServiceUnavailable"}' });
    await expect(apiClient.get("/api/payment/token/attempts")).rejects.toMatchObject({ status: 503 });
    expect(unavailable).toHaveBeenCalledOnce();

    vi.mocked(axios.request).mockResolvedValueOnce({ status: 500, data: "Internal server error" });
    await expect(apiClient.get("/api/debts")).rejects.toMatchObject({ status: 500 });
    expect(unavailable).toHaveBeenCalledOnce();

    vi.mocked(axios.request).mockResolvedValueOnce({ status: 422, data: '{"code":"VALIDATION"}' });
    await expect(apiClient.post("/api/debts", {})).rejects.toMatchObject({ status: 422 });
    expect(unavailable).toHaveBeenCalledOnce();
  } finally {
    window.removeEventListener("yalla:api-unavailable", unavailable);
  }
});

it("signals transport failures but ignores canceled API requests", async () => {
  const unavailable = vi.fn();
  window.addEventListener("yalla:api-unavailable", unavailable);
  try {
    vi.mocked(axios.request).mockRejectedValueOnce({ isAxiosError: true, code: "ERR_NETWORK" });
    await expect(apiClient.get("/api/debts")).rejects.toMatchObject({ code: "ERR_NETWORK" });
    expect(unavailable).toHaveBeenCalledOnce();

    vi.mocked(axios.request).mockRejectedValueOnce({ isAxiosError: true, code: "ERR_CANCELED" });
    await expect(apiClient.get("/api/debts")).rejects.toMatchObject({ code: "ERR_CANCELED" });
    expect(unavailable).toHaveBeenCalledOnce();
  } finally {
    window.removeEventListener("yalla:api-unavailable", unavailable);
  }
});
