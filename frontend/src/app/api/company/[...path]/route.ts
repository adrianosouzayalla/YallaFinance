import { companyProxy } from "@/features/settings/server/company-proxy";

export async function GET(request: Request, context: { params: Promise<{ path: string[] }> }) {
  return companyProxy(request, (await context.params).path);
}

export async function POST(request: Request, context: { params: Promise<{ path: string[] }> }) {
  return companyProxy(request, (await context.params).path);
}

export async function PUT(request: Request, context: { params: Promise<{ path: string[] }> }) {
  return companyProxy(request, (await context.params).path);
}

export async function DELETE(request: Request, context: { params: Promise<{ path: string[] }> }) {
  return companyProxy(request, (await context.params).path);
}
