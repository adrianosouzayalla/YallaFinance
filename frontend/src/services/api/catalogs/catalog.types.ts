export interface CategoryOption {
  id: string;
  code: string;
  name: string;
  tenantId: string;
  active: boolean;
}

export interface PaymentMethodOption {
  id: string;
  code: string;
  name: string;
  tenantId: string;
  active: boolean;
}

export interface FinancialCostCenter {
  externalReference: string;
  name: string;
}
export interface FinancialCustomer extends FinancialCostCenter {
  document: string | null;
  documentType?: "CPF" | "CNPJ" | null;
  email?: string | null;
  phone?: string | null;
}
export interface FinancialCatalogPage<T> {
  connectionId: string;
  items: T[];
  totalCount: number;
}
