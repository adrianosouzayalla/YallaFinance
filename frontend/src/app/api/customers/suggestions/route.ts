import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { customersServerApi } from "@/features/customers/api/customers.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    customersServerApi.suggestions(new URL(request.url).search, token, request.signal),
  );
}
