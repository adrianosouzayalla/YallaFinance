import { backendResponse } from "@/services/api/apiClient.server";

export async function POST(
  request: Request,
  context: RouteContext<"/api/payment/[token]/attempts">,
) {
  const { token } = await context.params;
  const idempotencyKey = request.headers.get("Idempotency-Key");
  const response = await backendResponse(
    `/api/payments/${encodeURIComponent(token)}/attempts`,
    undefined,
    {
      method: "POST",
      body: await request.text(),
      headers: idempotencyKey ? { "Idempotency-Key": idempotencyKey } : undefined,
      signal: request.signal,
    },
  );
  response.headers.set("Referrer-Policy", "no-referrer");
  return response;
}
