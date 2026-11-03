import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { jobsServerApi } from "@/features/jobs/api/jobs.server";
export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    jobsServerApi.executions(id, new URL(request.url).search, token, request.signal),
  );
}
