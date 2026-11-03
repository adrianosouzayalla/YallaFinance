"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { categoriesApi } from "../api/categories.client";
export function useCategories() {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["categories", tenantId, contextId],
    queryFn: ({ signal }) => categoriesApi.listCategories({ signal, contextId }),
  });
}
