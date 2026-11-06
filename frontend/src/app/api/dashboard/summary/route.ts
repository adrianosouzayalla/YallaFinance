import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { dashboardServerApi } from "@/services/api/dashboard/dashboardApi.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    dashboardServerApi.summary(token, request.signal),
  );
}
