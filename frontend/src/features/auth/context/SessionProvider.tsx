"use client";
import { createContext, useContext, useEffect, type PropsWithChildren } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { clearBrowserSessionData } from "../browserSession";

interface SessionContext {
  tenantId: string;
  contextId: string;
  userId?: string;
}
const Context = createContext<SessionContext | null>(null);

export function SessionProvider({
  children,
  session,
}: PropsWithChildren<{ session: SessionContext }>) {
  const queryClient = useQueryClient();
  useEffect(() => {
    try {
      const previous = window.sessionStorage.getItem("yalla-finance.context");
      if (previous !== session.contextId) {
        clearBrowserSessionData();
        window.sessionStorage.setItem("yalla-finance.context", session.contextId);
      }
    } catch {
      /* Session authority and cache isolation do not depend on browser storage. */
    }
    return () => {
      const filters = {
        predicate: (query: { queryKey: readonly unknown[] }) =>
          query.queryKey[2] === session.contextId,
      };
      void queryClient.cancelQueries(filters);
      queryClient.removeQueries(filters);
    };
  }, [session.contextId, queryClient]);
  return <Context.Provider value={session}>{children}</Context.Provider>;
}

export function useSessionContext(): SessionContext {
  const session = useContext(Context);
  if (!session) throw new Error("SessionProvider is required");
  return session;
}
