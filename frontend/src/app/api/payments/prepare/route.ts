import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { paymentBackend } from "@/features/payments/api/payments.server";

export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) => paymentBackend("prepare", token, { method: "POST", body: await request.text(), signal: request.signal }));
}

