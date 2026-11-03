import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { backendResponse } from "@/services/api/api-client.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) => backendResponse(
    `/api/auth/sessions${new URL(request.url).search}`, token, { signal: request.signal }));
}
