export type CompanySettingsGroup =
  "payments" | "session" | "user-registration" | "financial-settlement" | "job-retry";
export interface CompanySettingsByGroup {
  payments: { maximumLinkDays: string | null; publicOrigin: string | null };
  session: { sessionLifetimeMinutes: string | null };
  "user-registration": { initialPassword: string | null };
  "financial-settlement": { receiptTimeZoneId: string | null };
  "job-retry": { limits: Record<string, number> };
}
export interface CompanyPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}
export interface CompanyCatalogOption { id: string; code: string; name: string; active: boolean }
export interface CompanyConnectionSettingInput { group: string; key: string; value: string | null }
export interface CompanyConnectionSummary {
  id: string;
  name: string;
  provider: string;
  group: string;
  enabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}
export interface CompanyMapping {
  id: string;
  mappingType: string;
  internalValue: string;
  internalName: string | null;
  internalActive: boolean | null;
  externalValue: string;
  createdAt: string;
  updatedAt: string | null;
}
export interface CompanyPaymentMethod {
  id: string;
  paymentMethodId: string;
  paymentMethodName: string | null;
  paymentMethodActive: boolean | null;
  enabled: boolean;
  instrumentType?: string | null;
  internalDescription?: string | null;
  createdAt: string;
  updatedAt: string | null;
}
export interface CompanyPaymentMethodProviderConflict {
  paymentMethodId: string;
  provider: string;
  connectionName: string;
}
export interface CompanyConnectionSetting {
  group: string;
  key: string;
  value: string | null;
  configured: boolean;
  sensitive: boolean;
}
export interface CompanyConnectionDetail {
  connection: CompanyConnectionSummary;
  providerSupported: boolean;
  settings: CompanyConnectionSetting[];
  categoryMappings: CompanyPage<CompanyMapping>;
  receiptAccountMappings: CompanyPage<CompanyMapping>;
  paymentMethods: CompanyPage<CompanyPaymentMethod>;
  supportedPaymentInstruments?: string[];
  availablePaymentMethods?: CompanyPaymentMethodOption[];
  paymentMethodProviderConflicts?: CompanyPaymentMethodProviderConflict[];
}
export interface CompanyConnectionDraft {
  name: string;
  provider: string;
  group: string;
  enabled: boolean;
}
export interface CompanyMappingDraft { internalId: string; externalValue: string }
export interface CompanyPaymentMethodOption extends CompanyCatalogOption {
  instrumentType: string | null;
  internalDescription: string | null;
  canClassify: boolean;
}
export interface CompanyPaymentMethodDraft {
  paymentMethodId: string; enabled: boolean;
  instrumentType?: string | null; internalDescription?: string | null;
}
export type CompanySortDirection = "asc" | "desc";
export type CompanyMappingSort =
  "internalName" | "internalValue" | "externalValue" | "createdAt" | "updatedAt";
export interface CompanyConnectionFilter {
  search?: string;
  provider?: string;
  group?: string;
  enabled?: boolean;
  page: number;
  pageSize: number;
  sortBy: "name" | "provider" | "group" | "enabled" | "createdAt" | "updatedAt";
  sortDirection: CompanySortDirection;
}
export interface CompanyConnectionDetailFilter {
  categoryPage: number;
  receiptAccountPage: number;
  paymentMethodPage: number;
  pageSize: number;
  categorySortBy: CompanyMappingSort;
  categorySortDirection: CompanySortDirection;
  receiptAccountSortBy: CompanyMappingSort;
  receiptAccountSortDirection: CompanySortDirection;
  paymentMethodSortBy: "paymentMethodName" | "enabled" | "createdAt" | "updatedAt";
  paymentMethodSortDirection: CompanySortDirection;
}
export const DEFAULT_CONNECTION_FILTERS: CompanyConnectionFilter = {
  page: 1,
  pageSize: 20,
  sortBy: "name",
  sortDirection: "asc",
};
export const DEFAULT_DETAIL_FILTERS: CompanyConnectionDetailFilter = {
  categoryPage: 1,
  receiptAccountPage: 1,
  paymentMethodPage: 1,
  pageSize: 20,
  categorySortBy: "internalName",
  categorySortDirection: "asc",
  receiptAccountSortBy: "internalName",
  receiptAccountSortDirection: "asc",
  paymentMethodSortBy: "paymentMethodName",
  paymentMethodSortDirection: "asc",
};
