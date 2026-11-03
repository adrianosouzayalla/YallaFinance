import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { paymentBackend } from "@/features/payments/api/payments.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, async (token) => paymentBackend(`payment-requests/${encodeURIComponent(id)}`, token, { method: "GET", signal: request.signal }));
}

