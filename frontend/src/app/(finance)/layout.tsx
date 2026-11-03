import { redirect } from "next/navigation";
import { FinanceShell } from "@/components/finance/FinanceShell";
import type { FinanceSession } from "@/components/finance/FinanceSessionSummary";
import { readSession } from "@/features/auth/server/session";
import { SessionGuard } from "@/components/auth/SessionGuard";
import { SessionProvider } from "@/features/auth/context/SessionProvider";

export default async function FinanceLayout({ children }: LayoutProps<"/">) {
  const sessionProfile = await readSession();

  if (!sessionProfile) {
    redirect("/login");
  }

  const financeSession: FinanceSession = {
    canLogout: true,
    contextId: sessionProfile.contextId!,
    displayName: sessionProfile.displayName,
    organization:
      sessionProfile.tenants?.find((tenant) => tenant.id === sessionProfile.tenantId)?.name ??
      sessionProfile.organization,
    tenantId: sessionProfile.tenantId,
    tenants: sessionProfile.tenants,
    role: sessionProfile.role,
    environment: sessionProfile.environment,
    functionalityCodes: sessionProfile.functionalityCodes ?? [],
  };

  return (
    <SessionProvider
      key={sessionProfile.contextId}
      session={{ tenantId: sessionProfile.tenantId ?? "", contextId: sessionProfile.contextId!, userId: sessionProfile.userId }}
    >
      <SessionGuard>
        <FinanceShell session={financeSession}>{children}</FinanceShell>
      </SessionGuard>
    </SessionProvider>
  );
}
