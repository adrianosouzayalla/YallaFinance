import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { customersApi } from "@/services/api/customers/customersApi.client";
export function useCustomers(search = "", enabled = true) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["customers", tenantId, contextId, search],
    enabled,
    queryFn: ({ signal }) => customersApi.findCustomers(search, { signal, contextId }),
  });
}
