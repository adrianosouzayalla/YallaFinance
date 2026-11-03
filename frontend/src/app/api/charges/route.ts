import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { chargesServerApi } from "@/features/charges/api/charges.server";

export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) =>
    chargesServerApi.create(
      await request.text(),
      request.headers.get("Idempotency-Key"),
      token,
      request.signal,
    ),
  );
}
