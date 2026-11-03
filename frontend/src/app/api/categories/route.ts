import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { catalogsServerApi } from "@/features/catalogs/api/catalogs.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    catalogsServerApi.categories(token, request.signal),
  );
}
