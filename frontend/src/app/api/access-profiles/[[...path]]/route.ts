import { directoryProxy } from "@/features/users/server/directoryProxy";
type Context = { params: Promise<{ path?: string[] }> };
async function proxyAccessProfilesRequest(request: Request, context: Context) {
  return directoryProxy(request, "access-profiles", (await context.params).path ?? []);
}
export function GET(request: Request, context: Context) {
  return proxyAccessProfilesRequest(request, context);
}
export function POST(request: Request, context: Context) {
  return proxyAccessProfilesRequest(request, context);
}
export function PUT(request: Request, context: Context) {
  return proxyAccessProfilesRequest(request, context);
}
