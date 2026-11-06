import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { jobsServerApi } from "@/services/api/jobs/jobsApi.server";
export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    jobsServerApi.executions(id, new URL(request.url).search, token, request.signal),
  );
}
