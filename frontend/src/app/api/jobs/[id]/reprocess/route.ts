import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { jobsServerApi } from "@/services/api/jobs/jobsApi.server";

export async function POST(request: Request, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  return authenticatedResponse(request, (token) =>
    jobsServerApi.reprocess(id, token, request.signal),
  );
}
