import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { customersApi } from "@/services/api/customers/customersApi.client";
export function useFleetMovements(customerId: string, occurrenceDate: string) {
  const { tenantId, contextId } = useSessionContext();
  return useQuery({
    queryKey: ["movements", tenantId, contextId, customerId, occurrenceDate],
    enabled: Boolean(customerId && occurrenceDate),
    queryFn: ({ signal }) =>
      customersApi.findFleetMovements(customerId, occurrenceDate, { signal, contextId }),
  });
}
