"use client";

import { useEffect, useState } from "react";
import { ApiError } from "@/services/api/apiError";
import { authClientApi } from "@/services/api/auth/authApi.client";
import { useQueryClient } from "@tanstack/react-query";
import { clearBrowserSessionData } from "@/features/auth/browserSession";
import { discardLocalSessionAction } from "@/features/auth/actions";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { LoadingState } from "@/components/ui/LoadingState";

export function SessionGuard({ children }: React.PropsWithChildren) {
  const { contextId } = useSessionContext();
  return (
    <SessionValidation key={contextId} contextId={contextId}>
      {children}
    </SessionValidation>
  );
}

function SessionValidation({
  children,
  contextId,
}: React.PropsWithChildren<{ contextId: string }>) {
  const queryClient = useQueryClient();
  const [validating, setValidating] = useState(true);
  useEffect(() => {
    let active = true;
    let redirecting = false;
    function failure(event: Event) {
      redirecting = true;
      setValidating(true);
      const status = (event as CustomEvent<{ status: number }>).detail.status;
      void queryClient.cancelQueries();
      queryClient.clear();
      clearBrowserSessionData();
      if (status === 409) {
        window.location.reload();
        return;
      }
      window.location.replace(status === 401 ? "/login?error=session-expired" : "/not-found");
    }
    function apiUnavailable() {
      if (redirecting) return;
      redirecting = true;
      setValidating(true);
      void queryClient.cancelQueries();
      queryClient.clear();
      clearBrowserSessionData();
      void discardLocalSessionAction()
        .catch(() => undefined)
        .finally(() => window.location.replace("/login?error=api-unavailable"));
    }
    window.addEventListener("yalla:auth-failure", failure);
    window.addEventListener("yalla:api-unavailable", apiUnavailable);
    const controller = new AbortController();
    void authClientApi
      .session({ signal: controller.signal, contextId })
      .catch((error) => {
        if (
          error instanceof ApiError &&
          (error.status === 401 || error.status === 403 || error.code === "SESSION_CONTEXT_CHANGED")
        ) {
          failure(new CustomEvent("yalla:auth-failure", { detail: { status: error.status } }));
        }
      })
      .finally(() => {
        if (active && !redirecting) setValidating(false);
      });
    return () => {
      active = false;
      controller.abort();
      window.removeEventListener("yalla:auth-failure", failure);
      window.removeEventListener("yalla:api-unavailable", apiUnavailable);
    };
  }, [queryClient, contextId]);

  return (
    <>{validating ? <LoadingState mode="blocking" message="Validando sessão..." /> : children}</>
  );
}
