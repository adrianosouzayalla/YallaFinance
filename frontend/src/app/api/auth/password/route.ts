import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { deleteSession } from "@/features/auth/server/session";
import { backendResponse } from "@/services/api/api-client.server";
export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) => {
    const response = await backendResponse("/api/auth/password", token,
      { method: "POST", signal: request.signal, body: await request.text() });
    if (response.ok) await deleteSession();
    return response;
  });
}

