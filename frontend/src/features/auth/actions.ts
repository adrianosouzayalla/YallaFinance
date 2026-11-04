"use server";

import { redirect } from "next/navigation";
import { type LoginActionState } from "./authSession";
import { authServerApi } from "./api/auth.server";
import { ApiError } from "@/services/api/api-error";
import { createSession, deleteSession, readSession } from "./server/session";

export async function loginAction(
  _previousState: LoginActionState,
  formData: FormData,
): Promise<LoginActionState> {
  const email = String(formData.get("email") ?? "");
  const password = String(formData.get("password") ?? "");
  const tenantId = String(formData.get("tenantId") ?? "").trim() || undefined;
  // A ticket is sent only when the form is completing a forced password change (feature 28, phase 3).
  const ticket = String(formData.get("ticket") ?? "");
  if (ticket) return completeRequiredPasswordChange(formData, email, ticket);
  try {
    const result = await authServerApi.login({ email, password, tenantId });
    // Checked before tenant selection: the response also carries the ticket and may list companies.
    if (result.errorCode === "PASSWORD_CHANGE_REQUIRED")
      return {
        fieldErrors: {},
        formError: "Defina uma nova senha para continuar.",
        email,
        passwordChangeRequired: true,
        passwordChangeTicket: result.token ?? undefined,
      };
    if (!result.token && result.tenants?.length)
      return {
        fieldErrors: {},
        formError: "Selecione a empresa para continuar.",
        email,
        tenantOptions: result.tenants,
      };
    if (!result.token)
      return {
        fieldErrors: {},
        formError: "Não foi possível iniciar a sessão.",
        email,
        tenantOptions: _previousState.tenantOptions,
      };
    const expiresAt = result.expiresAt ? Date.parse(result.expiresAt) : NaN;
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      await authServerApi.logout(result.token);
      return { fieldErrors: {}, formError: "A API retornou uma sessão sem validade. Tente novamente.", email };
    }
    try { await createSession({
      displayName: result.displayName ?? email,
      token: result.token,
      expiresAt: new Date(expiresAt).toISOString(),
      tenantId: result.tenantId,
      tenants: result.tenants,
    }); } catch (error) {
      // If cookie creation fails, do not leave a newly issued session active.
      await authServerApi.logout(result.token);
      throw error;
    }
  } catch (error) {
    if (error instanceof ApiError && error.status === 429)
      return { fieldErrors: {}, formError: "Muitas tentativas de login. Aguarde alguns minutos e tente novamente.", email };
    return { fieldErrors: {}, formError: "Não foi possível iniciar a sessão. Confira o acesso e tente novamente.", email };
  }
  redirect("/home?toast=login-success");
}

async function completeRequiredPasswordChange(formData: FormData, email: string, ticket: string): Promise<LoginActionState> {
  const currentPassword = String(formData.get("password") ?? "");
  const newPassword = String(formData.get("newPassword") ?? "");
  const confirmation = String(formData.get("confirmPassword") ?? "");
  const stillPending = { fieldErrors: {}, email, passwordChangeRequired: true, passwordChangeTicket: ticket };
  if (newPassword !== confirmation)
    return { ...stillPending, formError: "A confirmação não confere com a nova senha." };
  try {
    const response = await authServerApi.changeRequiredPassword({ email, ticket, currentPassword, newPassword, confirmation });
    if (response.ok)
      return { fieldErrors: {}, email, formError: "Senha alterada. Entre com a nova senha.", passwordChanged: true };
    if (response.status === 401)
      return { ...stillPending, formError: "Senha atual incorreta ou troca expirada. Confira a senha ou entre novamente." };
    if (response.status === 409)
      return { fieldErrors: {}, email, formError: "Esta conta não precisa trocar a senha. Entre novamente." };
    if (response.status === 400)
      return { ...stillPending, formError: "Confira a nova senha: de 12 a 128 caracteres." };
    return { ...stillPending, formError: "Não foi possível trocar a senha. Tente novamente." };
  } catch {
    return { ...stillPending, formError: "Não foi possível trocar a senha. Tente novamente." };
  }
}

export async function logoutAction(): Promise<{ error: string }> {
  const session = await readSession();
  if (session) {
    try {
      const response = await authServerApi.logout(session.token);
      if (!response.ok && response.status !== 401)
        return { error: "Não foi possível encerrar a sessão. Tente novamente." };
    } catch { return { error: "Não foi possível encerrar a sessão. Tente novamente." }; }
  }
  await deleteSession();
  redirect("/login");
}

export async function discardLocalSessionAction(): Promise<void> {
  await deleteSession();
}
