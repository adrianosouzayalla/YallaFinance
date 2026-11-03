import { directoryProxy } from "@/features/users/server/directory-proxy";
type Context = { params: Promise<{ path?: string[] }> };
async function handle(request: Request, context: Context) {
  return directoryProxy(request, "users", (await context.params).path ?? []);
}
export { handle as GET, handle as POST, handle as PUT };

