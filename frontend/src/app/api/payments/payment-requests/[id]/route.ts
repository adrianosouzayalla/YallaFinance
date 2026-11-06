import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { paymentBackend } from "@/services/api/payments/paymentsApi.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, async (token) =>
    paymentBackend(`payment-requests/${encodeURIComponent(id)}`, token, {
      method: "GET",
      signal: request.signal,
    }),
  );
}
