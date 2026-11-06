import { useInfiniteQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { customersApi } from "@/services/api/customers/customersApi.client";
export function useCustomerContracts(customerId: string, occurrenceDate: string) {
  const { tenantId, contextId } = useSessionContext();
  return useInfiniteQuery({
    queryKey: ["customer-contracts", tenantId, contextId, customerId, occurrenceDate],
    enabled: Boolean(customerId && occurrenceDate),
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      customersApi.listContracts(customerId, pageParam, { signal, contextId }, occurrenceDate),
    getNextPageParam: (last) => (last.hasMore ? last.page + 1 : undefined),
  });
}
