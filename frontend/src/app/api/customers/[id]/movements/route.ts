import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { customersServerApi } from "@/features/customers/api/customers.server";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    customersServerApi.movements(id, new URL(request.url).search, token, request.signal),
  );
}
