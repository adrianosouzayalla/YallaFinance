import { authenticatedResponse } from "@/features/auth/server/session-boundary";

export async function GET(request: Request) {
  return authenticatedResponse(request, async (_token, session) => Response.json({
    authenticated: true,
    userId: session.userId,
    displayName: session.displayName,
    expiresAt: session.expiresAt,
    tenantId: session.tenantId,
    contextId: session.contextId,
    organization: session.organization,
  }, { headers: { "Cache-Control": "private, no-store" } }));
}
