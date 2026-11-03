import "server-only";
import { authenticatedResponse } from "@/features/auth/server/session-boundary";
import { backendResponse } from "@/services/api/api-client.server";

const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const groups = new Set(["payments", "session", "user-registration", "financial-settlement", "job-retry"]);
const mappingTypes = new Set(["Category", "ReceiptAccount"]);
const supportedPaymentInstruments = new Set(["Pix", "CreditCard", "Boleto"]);
const mappingSorts = ["internalName", "internalValue", "externalValue", "createdAt", "updatedAt"];
const oneOf = (values: string[]) => (value: string) => values.includes(value);
const positiveInteger = (value: string) => /^[1-9]\d*$/.test(value) && Number.isSafeInteger(Number(value));
const pageSize = (value: string) => positiveInteger(value) && Number(value) <= 100;
const direction = oneOf(["asc", "desc"]);
const listFilters: Record<string, (value: string) => boolean> = {
  search: () => true,
  provider: oneOf(["Nibo", "Sicoob", "Asaas"]),
  group: oneOf(["FinancialSystem", "PaymentProvider"]),
  enabled: oneOf(["true", "false"]),
  page: positiveInteger,
  pageSize,
  sortBy: oneOf(["name", "provider", "group", "enabled", "createdAt", "updatedAt"]),
  sortDirection: direction,
};
const detailFilters: Record<string, (value: string) => boolean> = {
  categoryPage: positiveInteger,
  receiptAccountPage: positiveInteger,
  paymentMethodPage: positiveInteger,
  pageSize,
  categorySortBy: oneOf(mappingSorts),
  categorySortDirection: direction,
  receiptAccountSortBy: oneOf(mappingSorts),
  receiptAccountSortDirection: direction,
  paymentMethodSortBy: oneOf(["paymentMethodName", "enabled", "createdAt", "updatedAt"]),
  paymentMethodSortDirection: direction,
};
const headers = { "Cache-Control": "private, no-store" };
type Route = { method: string; backendPath: string; allowed: string[]; bodyKind?: string };

function resolveRoute(method: string, path: string[]): Route | undefined {
  const joined = path.join("/");
  if (path.length === 2 && path[0] === "settings" && groups.has(path[1]))
    return { method, backendPath: joined, allowed: ["GET", "PUT"], bodyKind: path[1] };
  if (joined === "settings/catalogs/categories" || joined === "settings/catalogs/payment-methods")
    return { method, backendPath: joined, allowed: ["GET"] };
  if (joined === "connections")
    return { method, backendPath: joined, allowed: ["GET", "POST"], bodyKind: "connection" };
  if (path.length === 2 && path[0] === "connections" && guid.test(path[1]))
    return { method, backendPath: joined, allowed: ["GET", "PUT"], bodyKind: "connection" };
  if (path.length === 3 && path[0] === "connections" && guid.test(path[1]) && path[2] === "settings")
    return { method, backendPath: joined, allowed: ["PUT"], bodyKind: "provider-settings" };
  if (path.length === 4 && path[0] === "connections" && guid.test(path[1]) && path[2] === "mappings" && mappingTypes.has(path[3]))
    return { method, backendPath: joined, allowed: ["POST"], bodyKind: "mapping" };
  if (path.length === 5 && path[0] === "connections" && guid.test(path[1]) && path[2] === "mappings"
    && mappingTypes.has(path[3]) && guid.test(path[4]))
    return { method, backendPath: joined, allowed: ["PUT"], bodyKind: "mapping" };
  if (path.length === 4 && path[0] === "connections" && guid.test(path[1]) && path[2] === "mappings" && guid.test(path[3]))
    return { method, backendPath: joined, allowed: ["DELETE"] };
  if (path.length === 3 && path[0] === "connections" && guid.test(path[1]) && path[2] === "payment-methods")
    return { method, backendPath: joined, allowed: ["POST"], bodyKind: "payment-method" };
  if (path.length === 4 && path[0] === "connections" && guid.test(path[1]) && path[2] === "payment-methods" && guid.test(path[3]))
    return { method, backendPath: joined, allowed: ["PUT", "DELETE"], bodyKind: "payment-method" };
  return undefined;
}

function hasExactKeys(value: Record<string, unknown>, keys: string[]) {
  return Object.keys(value).length === keys.length && keys.every((key) => Object.hasOwn(value, key));
}

function validBody(kind: string, value: unknown, mappingType?: string): boolean {
  if (kind === "payments")
    return !!value && typeof value === "object" && hasExactKeys(value as Record<string, unknown>, ["maximumLinkDays", "publicOrigin"])
      && ["maximumLinkDays", "publicOrigin"].every((key) => (value as Record<string, unknown>)[key] === null || typeof (value as Record<string, unknown>)[key] === "string");
  if (kind === "session" || kind === "user-registration" || kind === "financial-settlement") {
    const key = kind === "session" ? "sessionLifetimeMinutes" : kind === "user-registration" ? "initialPassword" : "receiptTimeZoneId";
    return !!value && typeof value === "object" && hasExactKeys(value as Record<string, unknown>, [key])
      && ((value as Record<string, unknown>)[key] === null || typeof (value as Record<string, unknown>)[key] === "string");
  }
  if (kind === "connection") {
    return !!value && typeof value === "object"
      && hasExactKeys(value as Record<string, unknown>, ["name", "provider", "group", "enabled"])
      && typeof (value as Record<string, unknown>).name === "string"
      && typeof (value as Record<string, unknown>).provider === "string"
      && typeof (value as Record<string, unknown>).group === "string"
      && typeof (value as Record<string, unknown>).enabled === "boolean";
  }
  if (kind === "provider-settings") {
    if (!Array.isArray(value)) return false;
    const allowed = new Set([
      "Connection.BaseUrl", "Connection.CompanyId", "Authentication.ApiToken", "Connection.Environment",
      "Authentication.ClientId", "Authentication.TokenUrl", "Authentication.Scopes", "Authentication.CertificatePem",
      "Authentication.PrivateKeyPem", "Authentication.PrivateKeyPassword", "Pix.ReceiverKey",
      "Pix.ReceivedLookbackDays", "Transport.RequestTimeoutSeconds",
    ]);
    return value.every((item) => !!item && typeof item === "object"
      && hasExactKeys(item as Record<string, unknown>, ["group", "key", "value"])
      && typeof (item as Record<string, unknown>).group === "string"
      && typeof (item as Record<string, unknown>).key === "string"
      && ((item as Record<string, unknown>).value === null || typeof (item as Record<string, unknown>).value === "string")
      && allowed.has(`${(item as Record<string, unknown>).group}.${(item as Record<string, unknown>).key}`));
  }
  if (kind === "mapping")
    return !!value && typeof value === "object" && hasExactKeys(value as Record<string, unknown>, ["mappingType", "internalId", "externalValue"])
      && (value as Record<string, unknown>).mappingType === mappingType
      && typeof (value as Record<string, unknown>).internalId === "string" && guid.test((value as Record<string, unknown>).internalId as string)
      && typeof (value as Record<string, unknown>).externalValue === "string" && guid.test((value as Record<string, unknown>).externalValue as string);
  if (kind === "payment-method") {
    if (!value || typeof value !== "object") return false;
    const input = value as Record<string, unknown>;
    return Object.keys(input).every((key) => ["paymentMethodId", "enabled", "instrumentType", "internalDescription"].includes(key))
      && typeof input.paymentMethodId === "string" && guid.test(input.paymentMethodId)
      && typeof input.enabled === "boolean"
      && (input.instrumentType == null || supportedPaymentInstruments.has(input.instrumentType as string))
      && (input.internalDescription == null || typeof input.internalDescription === "string");
  }
  return false;
}

function unsupportedPaymentInstrument(value: unknown): boolean {
  if (!value || typeof value !== "object") return false;
  const instrumentType = (value as Record<string, unknown>).instrumentType;
  return typeof instrumentType === "string" && !supportedPaymentInstruments.has(instrumentType);
}

export async function companyProxy(request: Request, path: string[]) {
  const route = resolveRoute(request.method.toUpperCase(), path);
  if (!route)
    return Response.json({ detail: "Rota inválida." }, { status: 404, headers });
  if (!route.allowed.includes(request.method.toUpperCase()))
    return Response.json({ detail: "Método não permitido." }, { status: 405, headers: { ...headers, Allow: route.allowed.join(", ") } });

  const query = new URL(request.url).searchParams;
  if (request.method.toUpperCase() !== "GET" && query.size > 0)
    return Response.json({ detail: "Filtros invÃ¡lidos." }, { status: 400, headers });
  const filters = route.backendPath === "connections" ? listFilters
    : path.length === 2 && path[0] === "connections" ? detailFilters : {};
  for (const [key, value] of query) {
    if (query.getAll(key).length !== 1 || !Object.hasOwn(filters, key) || !filters[key](value))
      return Response.json({ detail: "Filtros inválidos." }, { status: 400, headers });
  }

  let body: string | undefined;
  if (request.method !== "GET" && request.method !== "DELETE") {
    let payload: unknown;
    try { payload = await request.clone().json(); }
    catch { return Response.json({ detail: "Conteúdo inválido." }, { status: 400, headers }); }
    if (route.bodyKind === "payment-method" && unsupportedPaymentInstrument(payload))
      return Response.json({ detail: "Esta forma de pagamento não é atendida por esta conexão." }, { status: 400, headers });
    if (!route.bodyKind || !validBody(route.bodyKind, payload, route.bodyKind === "mapping" ? path[3] : undefined))
      return Response.json({ detail: "Conteúdo inválido." }, { status: 400, headers });
    body = JSON.stringify(payload);
  }

  const backendPath = `/api/company/${route.backendPath}${query.size ? `?${query}` : ""}`;
  const response = await authenticatedResponse(request, (token) => backendResponse(backendPath, token, {
    method: request.method,
    body,
    signal: request.signal,
  }));
  response.headers.set("Cache-Control", headers["Cache-Control"]);
  return response;
}
