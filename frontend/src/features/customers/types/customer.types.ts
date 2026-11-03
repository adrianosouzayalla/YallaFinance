export interface Customer {
  customerId: string;
  tenantId: string;
  id: string;
  name: string;
  taxId: string;
  phone: string;
  email: string;
}

export interface FleetMovement {
  id: string;
  tenantId: string;
  customerId: string;
  occurrenceDate: string;
  plate: string;
  contract: string;
  product: "TRUST" | "RENT" | "";
  fleet: string;
  costCenter: string;
}
