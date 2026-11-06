import "server-only";
import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { backendResponse } from "@/services/api/apiClient.server";
const guid = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
export async function directoryProxy(
  request: Request,
  kind: "users" | "access-profiles",
  path: string[],
) {
  const route = path.join("/");
  const userRoutes = new RegExp(`^(?:|initial-password|${guid}(?:/(?:active|password))?)$`);
  const profileRoutes = new RegExp(`^(?:|functionalities|${guid}(?:/active)?)$`);
  if (!(kind === "users" ? userRoutes : profileRoutes).test(route))
    return Response.json({ detail: "Rota inválida." }, { status: 404 });
  if (kind === "users" && route === "initial-password" && request.method !== "GET")
    return Response.json(
      { detail: "Método não permitido." },
      { status: 405, headers: { Allow: "GET" } },
    );
  return authenticatedResponse(request, async (token) =>
    backendResponse(
      `/api/${kind}${route ? `/${route}` : ""}${new URL(request.url).search}`,
      token,
      {
        method: request.method,
        signal: request.signal,
        body: request.method === "GET" ? undefined : await request.text(),
      },
    ),
  );
}
