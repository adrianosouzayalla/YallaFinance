import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { customersServerApi } from "@/services/api/customers/customersApi.server";
export async function GET(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return authenticatedResponse(request, (token) =>
    customersServerApi.contracts(id, new URL(request.url).search, token, request.signal),
  );
}
