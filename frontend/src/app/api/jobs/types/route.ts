import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { jobsServerApi } from "@/services/api/jobs/jobsApi.server";
export async function GET(request: Request) {
  return authenticatedResponse(request, (token) => jobsServerApi.types(token, request.signal));
}
