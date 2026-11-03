"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import { debtsApi } from "@/features/debts/api/debts.client";
import type {
  Charge,
  ChargeDraft,
  ChargeItem,
  ChargeProcessing,
  CreateDirectChargeRequest,
  DirectChargeReceipt,
} from "@/features/charges/types/charge.types";
import { request } from "@/services/api/api-client";
import { ApiError } from "@/services/api/api-error";
import { ChargeIntegrationError } from "../charge-integration-error";
import { decimalToCents, formatExactCurrencyInput } from "@/utils/currency";

type ChargeResult = {
  customerName?: string | null;
  reference: string;
  processing?: ChargeProcessing | null;
  items?: ChargeItem[] | null;
  note?: string | null;
  financialIntegration?: {
    outcome: "Completed" | "Rejected" | "Indeterminate";
    errorCode?: string;
  };
  id: string;
  idempotencyKey: string;
  source: "Selected" | "Standalone" | "Direct";
  tenantId: string;
  customerId: string;
  debtIds: string[];
  amount: number;
  exactAmount?: string | null;
  categoryId: string;
  dueDate: string;
  providerExternalId: string;
  pixId: string;
  pixPayload: string;
  createdAt: string;
};

function toCharge(result: ChargeResult): Charge {
  return {
    id: result.id,
    reference: result.reference,
    idempotencyKey: result.idempotencyKey,
    source:
      result.source === "Selected"
        ? "selected"
        : result.source === "Direct"
          ? "direct"
          : "standalone",
    tenantId: result.tenantId,
    customerId: result.customerId,
    ...(typeof result.customerName === "string" ? { customerName: result.customerName } : {}),
    categoryId: result.categoryId,
    debtIds: result.debtIds,
    amount: result.amount,
    ...(result.exactAmount && decimalToCents(result.exactAmount)
      ? { formattedAmount: `R$ ${formatExactCurrencyInput(decimalToCents(result.exactAmount))}` }
      : {}),
    payment: { dueDate: result.dueDate, customerNote: result.note ?? "" },
    providerExternalId: result.providerExternalId,
    pixId: result.pixId,
    pixPayload: result.pixPayload,
    createdAt: result.createdAt,
    ...(result.financialIntegration ? { financialIntegration: result.financialIntegration } : {}),
    ...(result.items
      ? {
          items: result.items.map((item) => ({
            ...item,
            ...(item.exactAmount && decimalToCents(item.exactAmount)
              ? {
                  formattedAmount: `R$ ${formatExactCurrencyInput(decimalToCents(item.exactAmount))}`,
                }
              : {}),
          })),
        }
      : {}),
    ...(result.processing ? { processing: result.processing } : {}),
  };
}

function readDirectChargeReceipt(value: unknown): DirectChargeReceipt {
  const result = value as Partial<ChargeResult> | null;
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const validId = (id: unknown): id is string =>
    typeof id === "string" && uuid.test(id) && id !== "00000000-0000-0000-0000-000000000000";
  if (
    !result ||
    result.source !== "Direct" ||
    !validId(result.id) ||
    !validId(result.tenantId) ||
    !validId(result.customerId) ||
    typeof result.reference !== "string" ||
    !result.reference.trim() ||
    typeof result.idempotencyKey !== "string" ||
    !result.idempotencyKey ||
    typeof result.amount !== "number" ||
    !Number.isFinite(result.amount) ||
    result.amount <= 0 ||
    typeof result.dueDate !== "string" ||
    typeof result.createdAt !== "string" ||
    !Array.isArray(result.items) ||
    result.items.length === 0 ||
    !Array.isArray(result.debtIds) ||
    result.items.length !== result.debtIds.length ||
    !result.items.every(
      (item) =>
        item &&
        validId(item.id) &&
        validId(item.debtId) &&
        validId(item.categoryId) &&
        Number.isFinite(item.amount) &&
        item.amount > 0 &&
        Number.isInteger(item.ordinal) &&
        item.ordinal > 0 &&
        result.debtIds!.includes(item.debtId),
    )
  ) {
    throw new ApiError(
      "INVALID_DIRECT_CHARGE_RECEIPT",
      "Não foi possível confirmar os dados da cobrança gravada.",
      502,
    );
  }
  return toCharge(result as ChargeResult);
}

export const chargesApi = {
  async refreshDirectChargeStatus(
    id: string,
    options: SessionApiOptions,
  ): Promise<DirectChargeReceipt> {
    const receipt = readDirectChargeReceipt(
      await request<unknown>(
        `/api/charges/direct/${encodeURIComponent(id)}/status`,
        sessionRequestOptions(options),
      ),
    );
    if (receipt.id.toLowerCase() !== id.toLowerCase())
      throw new ApiError(
        "INVALID_DIRECT_CHARGE_RECEIPT",
        "Foi recebida uma resposta de outra cobrança. Tente novamente.",
        502,
      );
    return receipt;
  },
  async submitDirectCharge(
    payload: CreateDirectChargeRequest,
    idempotencyKey: string,
    options: SessionApiOptions,
  ): Promise<DirectChargeReceipt> {
    const receipt = await request<unknown>("/api/charges/direct", {
      method: "POST",
      ...sessionRequestOptions(options),
      headers: { ...sessionRequestOptions(options).headers, "Idempotency-Key": idempotencyKey },
      body: JSON.stringify(payload),
    });
    return readDirectChargeReceipt(receipt);
  },
  async getCharge(id: string, options: SessionApiOptions): Promise<Charge | null> {
    try {
      const result = await request<ChargeResult>(
        `/api/charges/${encodeURIComponent(id)}`,
        sessionRequestOptions(options),
      );
      return toCharge(result);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) return null;
      throw error;
    }
  },
  async createCharge(
    draft: ChargeDraft,
    idempotencyKey: string,
    options: SessionApiOptions,
  ): Promise<Charge> {
    const firstDebt =
      draft.source === "selected"
        ? await debtsApi.getRawDebt(draft.debtIds[0], options)
        : undefined;
    const categoryId =
      draft.source === "standalone" ? draft.standalone.categoryId : firstDebt?.categoryId;
    // Old drafts retain their original command fields solely for idempotent recovery.
    const legacyMethod =
      draft.source === "standalone"
        ? draft.standalone.paymentMethodId
        : draft.payment.paymentMethodId;
    const legacyFields = legacyMethod
      ? {
          paymentMethodId: legacyMethod,
          paymentMethod: draft.source === "selected" ? draft.payment.method : "Pix",
        }
      : {};
    const requestBody =
      draft.source === "selected"
        ? {
            source: draft.source,
            customerId: draft.customerId,
            debtIds: draft.debtIds,
            amount: draft.amount,
            categoryId,
            occurrenceDate: firstDebt?.occurrenceDate,
            descriptionDetail: firstDebt?.description,
            dueDate: draft.payment.dueDate,
            customerNote: draft.payment.customerNote,
            selectedMovementId: null,
            ...legacyFields,
          }
        : {
            source: draft.source,
            customerId: draft.customerId,
            debtIds: [],
            amount: draft.standalone.amount,
            categoryId,
            occurrenceDate: draft.standalone.occurrenceDate,
            descriptionDetail: draft.standalone.descriptionDetail,
            dueDate: draft.standalone.dueDate,
            customerNote: draft.standalone.customerNote,
            selectedMovementId: draft.standalone.selectedMovementId ?? null,
            ...legacyFields,
          };
    const result = await request<ChargeResult>("/api/charges", {
      method: "POST",
      ...sessionRequestOptions(options),
      headers: { ...sessionRequestOptions(options).headers, "Idempotency-Key": idempotencyKey },
      body: JSON.stringify(requestBody),
    });
    if (result.processing?.status === "NotScheduled") {
      throw new ChargeIntegrationError(
        toCharge(result),
        "Cobrança criada no Yalla, mas a integração não pôde ser agendada.",
      );
    }
    if (
      !result.processing &&
      result.financialIntegration &&
      result.financialIntegration.outcome !== "Completed"
    ) {
      const message =
        result.financialIntegration.outcome === "Indeterminate"
          ? "Cobrança criada no Yalla, mas a confirmação na integração financeira ficou indeterminada. Confira os recebimentos antes de tentar novamente."
          : `Cobrança criada no Yalla, mas a integração financeira foi rejeitada (${result.financialIntegration.errorCode ?? "erro desconhecido"}).`;
      throw new ChargeIntegrationError(toCharge(result), message);
    }
    return toCharge(result);
  },
};
