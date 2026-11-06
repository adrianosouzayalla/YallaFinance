import type { DebtCategory } from "@/services/api/debts/debt.types";
import type { FleetMovement } from "@/services/api/customers/customer.types";

export interface StandaloneChargeDraft {
  customerId: string;
  category: DebtCategory | "";
  categoryId?: string;
  amount: number;
  occurrenceDate: string;
  descriptionDetail: string;
  dueDate: string;
  customerNote: string;
  selectedMovementId?: string;
  paymentMethodId?: string;
}

export type FieldErrors = Partial<Record<keyof StandaloneChargeDraft, string>>;

export interface PaymentInput {
  /** Legacy drafts only; methods are now selected in Payments. */
  method?: "Pix";
  paymentMethodId?: string;
  dueDate: string;
  customerNote: string;
}

export type ChargeDraft =
  | {
      source: "selected";
      customerId: string;
      customerName: string;
      debtIds: string[];
      amount: number;
      payment: PaymentInput;
    }
  | {
      source: "standalone";
      customerId: string;
      customerName: string;
      standalone: StandaloneChargeDraft;
      movement: FleetMovement;
    };

export interface Charge {
  customerName?: string | null;
  reference: string;
  processing?: ChargeProcessing | null;
  items?: ChargeItem[];
  financialIntegration?: FinancialIntegration;
  customerId: string;
  id: string;
  idempotencyKey: string;
  source: ChargeDraft["source"] | "direct";
  tenantId: string;
  categoryId: string;
  paymentMethodId?: string;
  debtIds: string[];
  amount: number;
  payment: PaymentInput;
  formattedAmount?: string;
  providerExternalId: string;
  pixId: string;
  pixPayload: string;
  createdAt: string;
}

export interface ChargeItem {
  externalCostCenterId?: string | null;
  costCenterName?: string | null;
  exactAmount?: string | null;
  formattedAmount?: string;
  debt?: {
    contract: string;
    plate?: string | null;
    product?: string | null;
    fleet?: string | null;
    occurrenceDate: string;
    dueDate?: string | null;
    description: string;
    origin?: string;
  } | null;
  id: string;
  debtId: string;
  categoryId: string;
  amount: number;
  ordinal: number;
  provider?: string | null;
  providerExternalId?: string | null;
  financialIntegration?: FinancialIntegration | null;
}

interface FinancialIntegration {
  outcome: "Completed" | "Rejected" | "Indeterminate";
  errorCode?: string;
}

export interface ChargeProcessing {
  jobId?: string | null;
  status: "Pending" | "Processing" | "Success" | "Failed" | "NotScheduled";
  errorCode?: string | null;
  result?: {
    chargeId: string;
    outcome: "Completed" | "Partial" | "Rejected" | "Indeterminate";
    totalItems: number;
    confirmedItems: number;
    rejectedItems: number;
    indeterminateItems: number;
    items: {
      chargeItemId: string;
      outcome: "Completed" | "Rejected" | "Indeterminate";
      alreadyLinked: boolean;
      errorCode?: string | null;
    }[];
  } | null;
}

interface CreateDirectChargeItem {
  categoryId: string;
  categoryName: string;
  occurrenceDate: string;
  amount: string;
  descriptionDetail: string;
}

export interface CreateDirectChargeRequest {
  customerName: string;
  customerDocumentType: "CPF" | "CNPJ";
  customerEmail: string | null;
  customerPhone: string | null;
  costCenterReference: string;
  customerDocument: string;
  contract: string;
  plate: string;
  costCenterName: string;
  dueDate: string;
  customerNote: string;
  items: CreateDirectChargeItem[];
}

export type DirectChargeReceipt = Charge;
