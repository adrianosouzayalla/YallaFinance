import type {
  DirectChargeBasics,
  DirectDebtDraft,
} from "@/features/direct-charges/directChargeDraft";
import { buildDirectChargeRequest } from "@/features/direct-charges/directChargeDraft";
import type { Charge, CreateDirectChargeRequest } from "@/features/charges/types/charge.types";
import { centsToDecimal, decimalToCents, formatExactCurrencyInput } from "@/utils/currency";

export const financialConnectionId = "11111111-1111-4111-8111-111111111111";
export const directCategoryId = "22222222-2222-4222-8222-222222222222";
export const otherDirectCategoryId = "33333333-3333-4333-8333-333333333333";
export const directCategories = [
  {
    id: directCategoryId,
    code: "FINE",
    name: "Multa do banco",
    tenantId: "tenant-test",
    active: true,
  },
  {
    id: otherDirectCategoryId,
    code: "DAMAGE",
    name: "Avaria do banco",
    tenantId: "tenant-test",
    active: true,
  },
];
export const directCostCenters = {
  connectionId: financialConnectionId,
  totalCount: 2,
  items: [
    { externalReference: "center-a", name: "Operação do catálogo" },
    { externalReference: "center-b", name: "Administrativo do catálogo" },
  ],
};
export const directCustomers = {
  connectionId: financialConnectionId,
  totalCount: 1,
  items: [{ externalReference: "customer-a", name: "Cliente retornado", document: "12345678901" }],
};

export function createDirectChargeFixture() {
  return {
    basics: {
      customerName: "Cliente retornado",
      customerEmail: "",
      customerPhone: "",
      document: "12345678901",
      contract: "CT-TEST",
      plate: "ABC1D23",
      costCenter: "Operação do catálogo",
      costCenterReference: "center-a",
      dueDate: "2030-10-30",
      note: "",
    } satisfies DirectChargeBasics,
    debts: [
      {
        id: 1,
        categoryId: directCategoryId,
        category: "Multa do banco",
        occurrenceDate: "2030-10-20",
        amount: "150.00",
        detail: "Infração informada",
      },
      {
        id: 2,
        categoryId: otherDirectCategoryId,
        category: "Avaria do banco",
        occurrenceDate: "2030-10-20",
        amount: "300.00",
        detail: "Reparo informado",
      },
    ] satisfies DirectDebtDraft[],
  };
}

export function createDirectChargeResponse(payload: CreateDirectChargeRequest) {
  const exactAmount = centsToDecimal(
    payload.items
      .reduce((sum, item) => sum + BigInt(decimalToCents(item.amount)), BigInt(0))
      .toString(),
  )!;
  return {
    id: "44444444-4444-4444-8444-444444444444",
    reference: "1-29",
    source: "Direct" as const,
    idempotencyKey: "55555555-5555-4555-8555-555555555555",
    tenantId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    customerName: payload.customerName,
    categoryId: payload.items[0].categoryId,
    amount: Number(exactAmount),
    exactAmount,
    dueDate: payload.dueDate,
    providerExternalId: "",
    pixId: "",
    pixPayload: "",
    createdAt: "2030-10-23T12:00:00Z",
    note: payload.customerNote,
    processing: { status: "Pending" as const, jobId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd" },
    debtIds: payload.items.map(
      (_item, index) => `bbbbbbbb-bbbb-4bbb-8bbb-${String(index + 1).padStart(12, "0")}`,
    ),
    items: payload.items.map((item, index) => ({
      id: `eeeeeeee-eeee-4eee-8eee-${String(index + 1).padStart(12, "0")}`,
      debtId: `bbbbbbbb-bbbb-4bbb-8bbb-${String(index + 1).padStart(12, "0")}`,
      categoryId: item.categoryId,
      amount: Number(item.amount),
      exactAmount: item.amount,
      ordinal: index + 1,
      provider: null,
      providerExternalId: null,
      externalCostCenterId: payload.costCenterReference,
      costCenterName: payload.costCenterName,
      debt: {
        contract: payload.contract,
        plate: payload.plate,
        occurrenceDate: item.occurrenceDate,
        dueDate: payload.dueDate,
        description: `${item.categoryName.toLocaleUpperCase("pt-BR")} | ${item.descriptionDetail} | ${payload.contract}`,
        origin: "Yalla Finance",
      },
    })),
  };
}

export function createDirectChargeResult(payload: CreateDirectChargeRequest): Charge {
  const raw = createDirectChargeResponse(payload);
  return {
    id: raw.id,
    reference: raw.reference,
    idempotencyKey: raw.idempotencyKey,
    source: "direct",
    tenantId: raw.tenantId,
    customerId: raw.customerId,
    customerName: raw.customerName,
    categoryId: raw.categoryId,
    debtIds: raw.debtIds,
    amount: raw.amount,
    formattedAmount: `R$ ${formatExactCurrencyInput(decimalToCents(raw.exactAmount))}`,
    payment: { dueDate: raw.dueDate, customerNote: raw.note },
    createdAt: raw.createdAt,
    providerExternalId: "",
    pixId: "",
    pixPayload: "",
    processing: raw.processing,
    items: raw.items.map((item) => ({
      ...item,
      formattedAmount: `R$ ${formatExactCurrencyInput(decimalToCents(item.exactAmount))}`,
    })),
  };
}

const fixture = createDirectChargeFixture();
const initialRequest = buildDirectChargeRequest(fixture.basics, [fixture.debts[0]]);
export const directChargeResponse = createDirectChargeResponse(initialRequest);
export const directChargeReceipt = createDirectChargeResult(initialRequest);
