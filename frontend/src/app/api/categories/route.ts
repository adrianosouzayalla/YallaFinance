import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { catalogsServerApi } from "@/services/api/catalogs/catalogsApi.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    catalogsServerApi.categories(token, request.signal),
  );
}
