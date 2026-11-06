import { useMutation } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { companyApi } from "@/services/api/settings/settingsApi.client";
import { useInvalidateCompanyConnection } from "../../hooks/useCompanyConnection";
import type { CompanyMappingDraft } from "@/services/api/settings/types";

export function useNiboConnectionMutations(id: string | null) {
  const { contextId } = useSessionContext();
  const invalidate = useInvalidateCompanyConnection(id);
  const saveMapping = useMutation({
    mutationFn: (input: {
      type: "Category" | "ReceiptAccount";
      mappingId: string | null;
      mapping: CompanyMappingDraft;
    }) => companyApi.saveMapping(id!, input.type, input.mappingId, input.mapping, { contextId }),
    onSuccess: invalidate,
  });
  const removeMapping = useMutation({
    mutationFn: (mappingId: string) => companyApi.removeMapping(id!, mappingId, { contextId }),
    onSuccess: invalidate,
  });
  return { saveMapping, removeMapping };
}
