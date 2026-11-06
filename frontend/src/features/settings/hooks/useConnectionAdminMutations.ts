import { useMutation } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { companyApi } from "@/services/api/settings/settingsApi.client";
import { useInvalidateCompanyConnection } from "./useCompanyConnection";
import type {
  CompanyConnectionDraft,
  CompanyPaymentMethodDraft,
} from "@/services/api/settings/types";

export function useConnectionAdminMutations(id: string | null) {
  const { contextId } = useSessionContext();
  const invalidate = useInvalidateCompanyConnection(id);
  const createConnection = useMutation({
    mutationFn: (connection: CompanyConnectionDraft) =>
      companyApi.createConnection(connection, { contextId }),
    onSuccess: invalidate,
  });
  const saveConnection = useMutation({
    mutationFn: (connection: CompanyConnectionDraft) =>
      companyApi.saveConnection(id!, connection, { contextId }),
    onSuccess: invalidate,
  });
  return { createConnection, saveConnection };
}

export function usePaymentMethodMutations(id: string | null) {
  const { contextId } = useSessionContext();
  const invalidate = useInvalidateCompanyConnection(id);
  const save = useMutation({
    mutationFn: ({ linkId, draft }: { linkId: string | null; draft: CompanyPaymentMethodDraft }) =>
      companyApi.savePaymentMethod(id!, linkId, draft, { contextId }),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (linkId: string) => companyApi.removePaymentMethod(id!, linkId, { contextId }),
    onSuccess: invalidate,
  });
  return { save, remove };
}
