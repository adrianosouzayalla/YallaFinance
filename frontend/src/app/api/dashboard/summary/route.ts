import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { dashboardServerApi } from "@/features/dashboard/api/dashboard.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    dashboardServerApi.summary(token, request.signal),
  );
}
