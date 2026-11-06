export const debtStatuses = ["Open", "InCollection", "Paid", "Canceled"] as const;
export type DebtStatus = (typeof debtStatuses)[number];

export type DebtCategory = "Multa" | "Avaria" | "KM excedente" | "Guincho" | "Manutenção corretiva";

export interface Debt {
  id: string;
  category: DebtCategory;
  categoryId: string;
  tenantId: string;
  customerId: string;
  tenantName: string;
  contract: string;
  product: "TRUST" | "RENT" | "";
  plate: string;
  fleet: string;
  occurrenceDate: string;
  dueDate?: string | null;
  amount: number;
  status: DebtStatus;
  description: string;
  source: "Yalla Digital" | "Cobrança avulsa";
  providerExternalId?: string | null;
  chargeId?: string | null;
}

export type DebtSortKey =
  "category" | "tenantName" | "contract" | "plate" | "occurrenceDate" | "amount" | "status";

export interface DebtFilters {
  categories: string[];
  status: DebtStatus | "Todos";
  startDate: string;
  endDate: string;
  sortBy?: DebtSortKey;
  sortDirection: "asc" | "desc";
  page: number;
  pageSize: number;
}

interface DebtSummary {
  openCount: number;
  openAmount: number;
  chargingAmount: number;
  paidAmount: number;
}

export interface DebtListResult {
  items: Debt[];
  totalItems: number;
  page: number;
  pageSize: number;
  summary: DebtSummary;
}

export type SelectionValidation = { valid: true } | { valid: false; message: string };
