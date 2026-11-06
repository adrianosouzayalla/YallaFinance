import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { chargesApi } from "@/services/api/charges/chargesApi.client";
import type {
  Charge,
  ChargeDraft,
  CreateDirectChargeRequest,
} from "@/services/api/charges/charge.types";
import { ChargeIntegrationError } from "@/services/api/charges/chargeIntegrationError";

export function useSubmitDirectCharge() {
  const { tenantId, contextId } = useSessionContext();
  const client = useQueryClient();
  return useMutation({
    mutationKey: ["submit-direct-charge", tenantId, contextId],
    retry: false,
    mutationFn: ({
      payload,
      idempotencyKey,
      signal,
    }: {
      payload: CreateDirectChargeRequest;
      idempotencyKey: string;
      signal?: AbortSignal;
    }) => chargesApi.submitDirectCharge(payload, idempotencyKey, { contextId, signal }),
    onSuccess: (charge) => {
      client.setQueryData(["charge", tenantId, contextId, charge.id], charge);
      void client.invalidateQueries({ queryKey: ["debts", tenantId, contextId] });
      void client.invalidateQueries({ queryKey: ["dashboard", tenantId, contextId] });
    },
  });
}

export function useRefreshDirectChargeStatus() {
  const { tenantId, contextId } = useSessionContext();
  return useMutation({
    mutationKey: ["refresh-direct-charge-status", tenantId, contextId],
    retry: false,
    mutationFn: ({ chargeId, signal }: { chargeId: string; signal: AbortSignal }) =>
      chargesApi.refreshDirectChargeStatus(chargeId, { contextId, signal }),
  });
}

export function useCreateCharge() {
  const { tenantId, contextId } = useSessionContext();
  const client = useQueryClient();
  function preserve(charge: Charge) {
    client.setQueryData(["charge", tenantId, contextId, charge.id], charge);
    void client.invalidateQueries({ queryKey: ["debts", tenantId, contextId] });
    void client.invalidateQueries({ queryKey: ["dashboard", tenantId, contextId] });
  }
  return useMutation({
    retry: false,
    mutationFn: ({ draft, idempotencyKey }: { draft: ChargeDraft; idempotencyKey: string }) =>
      chargesApi.createCharge(draft, idempotencyKey, { contextId }),
    onSuccess: preserve,
    onError: (error) => {
      if (error instanceof ChargeIntegrationError) preserve(error.charge);
    },
  });
}
