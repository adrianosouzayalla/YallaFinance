import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { chargesServerApi } from "@/services/api/charges/chargesApi.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    chargesServerApi.detail(id, token, request.signal),
  );
}
