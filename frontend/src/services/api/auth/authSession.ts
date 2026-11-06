export interface LoginInput {
  email: string;
  password: string;
  tenantId?: string;
}
export interface TenantOption {
  id: string;
  name: string;
}
export interface Session {
  userId?: string;
  displayName: string;
  role?: string;
  organization?: string;
  environment?: string;
  expiresAt?: string;
  tenantId?: string;
  tenants?: TenantOption[];
  contextId?: string;
  functionalityCodes?: string[];
}
export interface LoginActionState {
  fieldErrors: Partial<Record<keyof LoginInput, string>>;
  formError: string;
  email?: string;
  tenantOptions?: TenantOption[];
  // Forced password change (feature 28, phase 3): the form asks for the new password; the ticket is single-purpose.
  passwordChangeRequired?: boolean;
  passwordChangeTicket?: string;
  passwordChanged?: boolean;
}
export const INITIAL_LOGIN_STATE: LoginActionState = { fieldErrors: {}, formError: "" };
export const SESSION_COOKIE = "yalla-finance-session";
