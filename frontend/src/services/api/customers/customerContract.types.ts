export interface CustomerContract {
  id: string;
  number: string;
  plate: string | null;
  product: string | null;
  fleet: string | null;
}
export interface CustomerContractPage {
  items: CustomerContract[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}
