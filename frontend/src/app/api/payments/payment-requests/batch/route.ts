import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { paymentBackend } from "@/services/api/payments/paymentsApi.server";

export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) =>
    paymentBackend("payment-requests/batch", token, {
      method: "POST",
      body: await request.text(),
      signal: request.signal,
    }),
  );
}
