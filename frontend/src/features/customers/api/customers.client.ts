"use client";

import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";

import type { Customer, FleetMovement } from "@/features/customers/types/customer.types";
import { request } from "@/services/api/api-client";
import type { CustomerContractPage } from "../types/customer-contract.types";

export const customersApi = {
  async suggestCustomers(
    term: string,
    options: SessionApiOptions,
    source?: "persisted",
  ): Promise<Customer[]> {
    return request<Customer[]>(
      `/api/customers/suggestions?search=${encodeURIComponent(term)}${source ? `&source=${source}` : ""}`,
      sessionRequestOptions(options),
    );
  },
  listContracts: (
    customerId: string,
    page: number,
    options: SessionApiOptions,
    occurrenceDate?: string,
  ) =>
    request<CustomerContractPage>(
      `/api/customers/${encodeURIComponent(customerId)}/contracts?page=${page}&pageSize=20${occurrenceDate ? `&occurrenceDate=${encodeURIComponent(occurrenceDate)}` : ""}`,
      sessionRequestOptions(options),
    ),
  async findCustomers(term: string, options: SessionApiOptions): Promise<Customer[]> {
    return request<Customer[]>(
      `/api/customers?search=${encodeURIComponent(term)}`,
      sessionRequestOptions(options),
    );
  },
  async findFleetMovements(
    customerId: string,
    occurrenceDate: string,
    options: SessionApiOptions,
  ): Promise<FleetMovement[]> {
    return request<FleetMovement[]>(
      `/api/customers/${encodeURIComponent(customerId)}/movements?occurrenceDate=${occurrenceDate}`,
      sessionRequestOptions(options),
    );
  },
};
