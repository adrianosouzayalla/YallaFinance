export interface PaymentCharge {
  reference: string;
  id: string;
  customerName: string;
  contract: string;
  amount: number;
  createdAt: string;
  dueDate: string;
  eligible: boolean;
  blockReason: string | null;
  settled?: boolean;
  receivedAmount?: number;
  receivedAt?: string | null;
  settledAt?: string | null;
  receipts?: PaymentReceipt[] | null;
  activePaymentRequest?: { id: string; status: string; createdAt: string; expiresAt: string } | null;
  activity?: PaymentActivitySummary | null;
}
export interface PaymentActivitySummary {
  latestRequest?: { id: string; status: string; createdAt: string; expiresAt: string; expiredAt: string | null; canceledAt: string | null; completedAt: string | null; completionReason: string | null; authorizedPaymentMethods?: string[] | null } | null;
  latestAttempt?: { status: string; provider: string; paymentMethodName: string; createdAt: string; updatedAt: string | null; expiresAt: string | null; issuanceBlocked: boolean; failureCode: string | null; failureMessage: string | null; boletoPdfUrl?: string | null } | null;
  openRiskAttempt?: { status: string; provider: string; paymentMethodName: string; createdAt: string; updatedAt: string | null; expiresAt: string | null; issuanceBlocked: boolean; failureCode: string | null; failureMessage: string | null; boletoPdfUrl?: string | null } | null;
}
export interface PaymentReceipt {
  id: string;
  endToEndId: string | null;
  providerTransactionId?: string | null;
  amount: number;
  receivedAt: string;
  paymentMethodName: string;
  provider: string;
}
export type PaymentChargeStatusFilter = "awaitingPayment" | "expired" | "available";
export interface PaymentChargeFilters { startDate: string; endDate: string; page: number; pageSize: number; sortBy?: string; sortDirection?: "asc" | "desc"; paymentStatus?: PaymentChargeStatusFilter; reference?: string }
export interface PaymentChargeList { items: PaymentCharge[]; page: number; pageSize: number; totalItems: number; totalAmount: number; awaitingPaymentCount: number; expiredPaymentCount: number; availablePaymentCount: number }
export interface PaymentMethodOption {
  id: string;
  name: string;
  code: string;
  instrumentType?: string | null;
  internalDescription?: string | null;
}
export interface PaymentPreparation { charges: PaymentCharge[]; methods: PaymentMethodOption[]; maximumLinkDays: number }
export interface PaymentRequest { id: string; chargeId: string; status: string; expiresAt: string; createdAt: string; replayed: boolean }
export interface PaymentBatchInput {
  items: { chargeId: string; idempotencyKey: string }[];
  paymentMethodIds: string[]; expiresAt: string; note: string;
}
export interface PaymentBatchResult { items: { chargeId: string; request: PaymentRequest | null; errorCode: string | null; message: string | null }[] }
export interface PublicPaymentAttempt {
  status: string;
  amount: number;
  expiresAt: string | null;
  brCode: string | null;
  location: string | null;
  canRetry: boolean;
  message: string | null;
  paymentMethodCode?: string | null;
  instrumentType?: string | null;
  boleto?: PublicBoletoDetails | null;
}
export interface PublicBoletoDetails {
  identificationField?: string | null;
  barcode?: string | null;
  bankSlipNumber?: string | null;
  bankSlipUrl?: string | null;
  invoiceUrl?: string | null;
}
export interface PublicPaymentCheckout {
  status: string;
  hasReceivedPayment?: boolean;
  receivedAmount?: number;
  receivedAt?: string | null;
  settled?: boolean;
  settledAt?: string | null;
  receipts?: { amount: number; receivedAt: string; paymentMethodName: string }[] | null;
  expiresAt: string;
  amount: number;
  dueDate: string;
  contractNumber: string;
  methods: PublicPaymentMethod[];
  attempt: PublicPaymentAttempt | null;
  cardRequiredFields?: string[];
}
export interface PublicPaymentMethod { code: string; name: string; instrumentType?: string | null }
export interface PublicCardPaymentInput {
  holderName: string;
  number: string;
  expiryMonth: string;
  expiryYear: string;
  securityCode: string;
  sameHolderAsCustomer: boolean;
  submissionId: string;
  holder?: { name?: string; email?: string; document?: string; postalCode?: string; addressNumber?: string; phone?: string };
}
