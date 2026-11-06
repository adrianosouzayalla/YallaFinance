import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { backendResponse } from "@/services/api/apiClient.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    backendResponse(`/api/auth/sessions${new URL(request.url).search}`, token, {
      signal: request.signal,
    }),
  );
}
