import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { paymentBackend } from "@/services/api/payments/paymentsApi.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, async (token) =>
    paymentBackend("charges" + new URL(request.url).search, token, { signal: request.signal }),
  );
}
