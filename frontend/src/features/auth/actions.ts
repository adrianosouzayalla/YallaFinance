"use server";

import { redirect } from "next/navigation";
import { type LoginActionState } from "./authSession";
import { authServerApi } from "./api/auth.server";
import { createSession, deleteSession, readSession } from "./server/session";

export async function loginAction(
  _previousState: LoginActionState,
  formData: FormData,
): Promise<LoginActionState> {
  const email = String(formData.get("email") ?? "");
  const password = String(formData.get("password") ?? "");
  const tenantId = String(formData.get("tenantId") ?? "").trim() || undefined;
  try {
    const result = await authServerApi.login({ email, password, tenantId });
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
  } catch {
    return { fieldErrors: {}, formError: "Não foi possível iniciar a sessão. Confira o acesso e tente novamente.", email };
  }
  redirect("/home?toast=login-success");
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
