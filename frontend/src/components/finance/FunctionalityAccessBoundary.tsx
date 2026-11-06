"use client";

import type { PropsWithChildren } from "react";
import { usePathname } from "next/navigation";
import type { FinanceSession } from "./FinanceSessionSummary";
import { AccessDeniedPage } from "./AccessDeniedPage";
import { HomeFallback } from "@/features/dashboard/components/HomeFallback";
import { getRequiredFunctionality } from "./navigation";

export function FunctionalityAccessBoundary({
  children,
  session,
}: PropsWithChildren<{ session?: FinanceSession }>) {
  const pathname = usePathname();
  const required = getRequiredFunctionality(pathname);
  const functionalityCodes = session?.functionalityCodes ?? [];
  if (pathname === "/home" || pathname === "/")
    return functionalityCodes.includes("DEBTS") ? <>{children}</> : <HomeFallback />;
  if (!required || functionalityCodes.includes(required)) return <>{children}</>;
  return <AccessDeniedPage />;
}
