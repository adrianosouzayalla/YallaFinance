import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { chargesApi } from "@/services/api/charges/chargesApi.client";

export function useCharge(id: string | null) {
  const { tenantId, contextId } = useSessionContext();
  const query = useQuery({
    queryKey: ["charge", tenantId, contextId, id],
    enabled: Boolean(id),
    // Reuse the complete POST response for immediate success; a fresh tab/refresh still queries the API.
    staleTime: 60_000,
    queryFn: ({ signal }) => chargesApi.getCharge(id!, { signal, contextId }),
  });
  return query;
}
