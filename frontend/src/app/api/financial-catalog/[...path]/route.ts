import { authenticatedResponse } from "@/features/auth/server/sessionBoundary";
import { catalogsServerApi } from "@/services/api/catalogs/catalogsApi.server";

type Context = { params: Promise<{ path: string[] }> };
async function routeFinancialCatalogRequest(request: Request, context: Context) {
  const path = (await context.params).path.join("/");
  return authenticatedResponse(request, async (token) => {
    if (request.method === "POST" && path === "customers/lookup")
      return catalogsServerApi.financialCustomers(await request.text(), token, request.signal);
    if (request.method === "GET" && path === "cost-centers") {
      const query = new URL(request.url).searchParams;
      const skip = Number(query.get("skip") ?? 0);
      const take = Number(query.get("take") ?? 100);
      if (
        !Number.isSafeInteger(skip) ||
        skip < 0 ||
        !Number.isInteger(take) ||
        take < 1 ||
        take > 500
      )
        return Response.json(
          { code: "INVALID_PAGINATION", detail: "Paginação inválida." },
          { status: 400 },
        );
      return catalogsServerApi.financialCostCenters(skip, take, token, request.signal);
    }
    return Response.json(
      { code: "NOT_FOUND", detail: "Consulta não encontrada." },
      { status: 404 },
    );
  });
}
export function GET(request: Request, context: Context) {
  return routeFinancialCatalogRequest(request, context);
}
export function POST(request: Request, context: Context) {
  return routeFinancialCatalogRequest(request, context);
}
