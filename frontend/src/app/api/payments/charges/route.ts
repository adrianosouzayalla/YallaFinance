import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { paymentBackend } from "@/features/payments/api/payments.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, async (token) => paymentBackend("charges" + new URL(request.url).search, token, { signal: request.signal }));
}

