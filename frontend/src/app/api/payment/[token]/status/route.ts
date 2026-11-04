import { backendResponse } from "@/services/api/api-client.server";

export async function GET(request: Request, context: RouteContext<"/api/payment/[token]/status">) {
  const { token } = await context.params;
  const response = await backendResponse(`/api/payments/${encodeURIComponent(token)}/status`, undefined, {
    method: "GET",
    signal: request.signal,
  });
  response.headers.set("Referrer-Policy", "no-referrer");
  return response;
}
