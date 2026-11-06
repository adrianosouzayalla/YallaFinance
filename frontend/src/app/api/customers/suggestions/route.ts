import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { customersServerApi } from "@/services/api/customers/customersApi.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    customersServerApi.suggestions(new URL(request.url).search, token, request.signal),
  );
}
