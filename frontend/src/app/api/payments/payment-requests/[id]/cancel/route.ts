import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { paymentBackend } from "@/services/api/payments/paymentsApi.server";

export async function POST(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, async (token) =>
    paymentBackend(`payment-requests/${encodeURIComponent(id)}/cancel`, token, {
      method: "POST",
      signal: request.signal,
    }),
  );
}
