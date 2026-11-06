import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { chargesServerApi } from "@/services/api/charges/chargesApi.server";

export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) =>
    chargesServerApi.submitDirectCharge(
      await request.text(),
      request.headers.get("Idempotency-Key"),
      token,
      request.signal,
    ),
  );
}
