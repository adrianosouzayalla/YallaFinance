import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { jobsServerApi } from "@/features/jobs/api/jobs.server";
export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    jobsServerApi.list(new URL(request.url).search, token, request.signal),
  );
}
