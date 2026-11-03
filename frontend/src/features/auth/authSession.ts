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
}
export const INITIAL_LOGIN_STATE: LoginActionState = { fieldErrors: {}, formError: "" };
export const SESSION_COOKIE = "yalla-finance-session";
export const DEMO_LOGIN = "admin@yalla.com.br";
export const DEMO_LOGIN_PASSWORD = "Yalla@2026";
