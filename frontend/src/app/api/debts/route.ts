import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { debtsServerApi } from "@/features/debts/api/debts.server";

export async function GET(request: Request) {
  return authenticatedResponse(request, (token) =>
    debtsServerApi.list(new URL(request.url).search, token, request.signal),
  );
}

export async function POST(request: Request) {
  return authenticatedResponse(request, async (token) =>
    debtsServerApi.createManualDebt(await request.text(), token, request.signal),
  );
}
