import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { debtsServerApi } from "@/features/debts/api/debts.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    debtsServerApi.detail(id, token, request.signal),
  );
}
