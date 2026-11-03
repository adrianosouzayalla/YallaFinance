import "server-only";
import { deleteSession, readSession } from "./session";
import { SESSION_CONTEXT_HEADER } from "../api/session-options";
import type { ServerSession } from "./session-cookie";

const headers = { "Cache-Control": "private, no-store" };

function requestOrigin(request: Request): string | null {
  try {
    const url = new URL(request.url);
    // Dynamic serving address: no fixed domain/IP allowlist or database parameter.
    // The browser sets Host; Next's request URL supplies the serving protocol.
    const host = request.headers.get("host") ?? url.host;
    return new URL(`${url.protocol}//${host}`).origin;
  } catch { return null; }
}

export async function authenticatedResponse(
  request: Request,
  operation: (token: string, session: ServerSession) => Promise<Response>,
): Promise<Response> {
  const session = await readSession().catch(() => undefined);
  if (session === undefined)
    return Response.json({ code: "SESSION_UNAVAILABLE", detail: "Não foi possível consultar a sessão. Tente novamente." }, { status: 503, headers });
  if (!session) {
    await deleteSession();
    return Response.json({ code: "SESSION_EXPIRED", detail: "Sua sessão expirou." }, { status: 401, headers });
  }
  if (request.method !== "GET") {
    const origin = request.headers.get("origin");
    if (
      !origin ||
      origin !== requestOrigin(request) ||
      request.headers.get("sec-fetch-site") === "cross-site"
    ) {
      return Response.json(
        { code: "INVALID_ORIGIN", detail: "Origem da operação inválida." },
        { status: 403, headers },
      );
    }
  }
  if (!session.contextId || request.headers.get(SESSION_CONTEXT_HEADER) !== session.contextId) {
    return Response.json(
      { code: "SESSION_CONTEXT_CHANGED", detail: "O contexto da sessão mudou. Atualize a página." },
      { status: 409, headers },
    );
  }
  try {
    const response = await operation(session.token, session);
    if (response.status === 401) await deleteSession();
    return response;
  } catch {
    return Response.json(
      { code: "API_UNAVAILABLE", detail: "Não foi possível acessar a API." },
      { status: 502, headers },
    );
  }
}
