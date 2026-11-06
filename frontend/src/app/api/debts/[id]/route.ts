import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { debtsServerApi } from "@/services/api/debts/debtsApi.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    debtsServerApi.detail(id, token, request.signal),
  );
}
