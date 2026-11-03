import { useMutation } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { companyApi } from "../company.client";
import { useInvalidateCompanyConnection } from "./useCompanyConnection";
import type { CompanyConnectionSettingInput } from "../types";

export function useSaveConnectionSettings(id: string | null) {
  const { contextId } = useSessionContext();
  const invalidate = useInvalidateCompanyConnection(id);
  return useMutation({
    mutationFn: (settings: CompanyConnectionSettingInput[]) => companyApi.saveConnectionSettings(id!, settings, { contextId }),
    onSuccess: invalidate,
  });
}
