import { directoryProxy } from "@/features/users/server/directoryProxy";
type Context = { params: Promise<{ path?: string[] }> };
async function proxyUsersRequest(request: Request, context: Context) {
  return directoryProxy(request, "users", (await context.params).path ?? []);
}
export function GET(request: Request, context: Context) {
  return proxyUsersRequest(request, context);
}
export function POST(request: Request, context: Context) {
  return proxyUsersRequest(request, context);
}
export function PUT(request: Request, context: Context) {
  return proxyUsersRequest(request, context);
}
