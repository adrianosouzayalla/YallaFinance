import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { jobsServerApi } from "@/features/jobs/api/jobs.server";
export async function GET(request: Request) {
  return authenticatedResponse(request, (token) => jobsServerApi.types(token, request.signal));
}
