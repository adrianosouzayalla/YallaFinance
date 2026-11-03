import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { chargesServerApi } from "@/features/charges/api/charges.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    chargesServerApi.detail(id, token, request.signal),
  );
}
