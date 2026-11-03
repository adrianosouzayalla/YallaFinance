import { sessionRequestOptions, type SessionApiOptions } from "@/features/auth/api/session-options";
import { request } from "@/services/api/api-client";
import type {
  CompanyConnectionDetail,
  CompanyConnectionDetailFilter,
  CompanyConnectionFilter,
  CompanyConnectionSummary,
  CompanyCatalogOption,
  CompanyConnectionDraft,
  CompanyConnectionSettingInput,
  CompanyMappingDraft,
  CompanyPaymentMethodDraft,
  CompanyPage,
  CompanySettingsByGroup,
  CompanySettingsGroup,
} from "./types";

function queryString(filters: object): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== "") query.set(key, String(value));
  }
  return query.toString();
}
export const companyApi = {
  getSettings<T extends CompanySettingsGroup>(group: T, options: SessionApiOptions) {
    return request<CompanySettingsByGroup[T]>(
      `/api/company/settings/${group}`,
      sessionRequestOptions(options),
    );
  },
  saveSettings<T extends CompanySettingsGroup>(group: T, values: Partial<CompanySettingsByGroup[T]>, options: SessionApiOptions) {
    return request<string>(`/api/company/settings/${group}`, {
      ...sessionRequestOptions(options), method: "PUT", body: JSON.stringify(values),
    });
  },
  listCategories(options: SessionApiOptions) {
    return request<CompanyCatalogOption[]>("/api/company/settings/catalogs/categories", sessionRequestOptions(options));
  },
  listPaymentMethods(options: SessionApiOptions) {
    return request<CompanyCatalogOption[]>("/api/company/settings/catalogs/payment-methods", sessionRequestOptions(options));
  },
  listConnections(filters: CompanyConnectionFilter, options: SessionApiOptions) {
    return request<CompanyPage<CompanyConnectionSummary>>(
      `/api/company/connections?${queryString(filters)}`,
      sessionRequestOptions(options),
    );
  },
  getConnection(id: string, filters: CompanyConnectionDetailFilter, options: SessionApiOptions) {
    return request<CompanyConnectionDetail>(
      `/api/company/connections/${encodeURIComponent(id)}?${queryString(filters)}`,
      sessionRequestOptions(options),
    );
  },
  createConnection(connection: CompanyConnectionDraft, options: SessionApiOptions) {
    return request<string>("/api/company/connections", {
      ...sessionRequestOptions(options), method: "POST", body: JSON.stringify(connection),
    });
  },
  saveConnection(id: string, connection: CompanyConnectionDraft, options: SessionApiOptions) {
    return request<string>(`/api/company/connections/${encodeURIComponent(id)}`, {
      ...sessionRequestOptions(options), method: "PUT", body: JSON.stringify(connection),
    });
  },
  saveConnectionSettings(id: string, settings: CompanyConnectionSettingInput[], options: SessionApiOptions) {
    return request<string>(`/api/company/connections/${encodeURIComponent(id)}/settings`, {
      ...sessionRequestOptions(options), method: "PUT", body: JSON.stringify(settings),
    });
  },
  saveMapping(id: string, mappingType: "Category" | "ReceiptAccount", mappingId: string | null,
    mapping: CompanyMappingDraft, options: SessionApiOptions) {
    const base = `/api/company/connections/${encodeURIComponent(id)}/mappings/${mappingType}`;
    const path = mappingId ? `${base}/${encodeURIComponent(mappingId)}` : base;
    return request<string>(path, {
      ...sessionRequestOptions(options), method: mappingId ? "PUT" : "POST",
      body: JSON.stringify({ ...mapping, mappingType }),
    });
  },
  removeMapping(id: string, mappingId: string, options: SessionApiOptions) {
    return request<string>(`/api/company/connections/${encodeURIComponent(id)}/mappings/${encodeURIComponent(mappingId)}`, {
      ...sessionRequestOptions(options), method: "DELETE",
    });
  },
  savePaymentMethod(id: string, linkId: string | null, link: CompanyPaymentMethodDraft, options: SessionApiOptions) {
    const base = `/api/company/connections/${encodeURIComponent(id)}/payment-methods`;
    const path = linkId ? `${base}/${encodeURIComponent(linkId)}` : base;
    return request<string>(path, {
      ...sessionRequestOptions(options), method: linkId ? "PUT" : "POST",
      body: JSON.stringify(link),
    });
  },
  removePaymentMethod(id: string, linkId: string, options: SessionApiOptions) {
    return request<string>(`/api/company/connections/${encodeURIComponent(id)}/payment-methods/${encodeURIComponent(linkId)}`, {
      ...sessionRequestOptions(options), method: "DELETE",
    });
  },
};
