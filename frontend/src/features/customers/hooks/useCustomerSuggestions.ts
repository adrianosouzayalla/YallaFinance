"use client";
import { useQuery } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { customersApi } from "../api/customers.client";

export function useCustomerSuggestions(input: string, enabled = true, source?: "persisted") {
  const { tenantId, contextId } = useSessionContext();
  const search = input.trim();
  const debounced = useDebouncedValue(search);
  const ready = enabled && search.length >= 3 && search === debounced;
  const query = useQuery({
    // Change the key immediately to detach/cancel the previous request while typing.
    queryKey: ["customer-suggestions", tenantId, contextId, search, source],
    enabled: ready,
    queryFn: ({ signal }) =>
      source
        ? customersApi.suggestCustomers(search, { signal, contextId }, source)
        : customersApi.suggestCustomers(search, { signal, contextId }),
  });
  return {
    ...query,
    items: ready ? (query.data ?? []) : [],
    searching: enabled && search.length >= 3 && (!ready || query.isFetching),
  };
}
