"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from "@mui/material";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import PersonAddAltOutlinedIcon from "@mui/icons-material/PersonAddAltOutlined";
import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { LoadingState } from "@/components/ui/LoadingState";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { directoryApi, directoryError } from "../directory.client";
import type { DirectoryPage, ProfileItem, UserItem } from "../types";
import { UserEditor } from "./UserEditor";
import { UserPasswordDialog } from "./UserPasswordDialog";

export function UserFormPage({ userId }: { userId?: string }) {
  const { tenantId, contextId, userId: currentUserId } = useSessionContext();
  const router = useRouter();
  const cache = useQueryClient();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [reset, setReset] = useState(false);
  const [confirmActive, setConfirmActive] = useState(false);
  const [manual, setManual] = useState(false);
  const query = useQuery({
    queryKey: ["user-form", tenantId, contextId, userId],
    queryFn: async ({ signal }) => {
      const profiles: ProfileItem[] = [];
      let page = 1;
      while (true) {
        const result = await directoryApi.get<DirectoryPage<ProfileItem>>(`/api/access-profiles?active=true&page=${page}&pageSize=100`, contextId, signal);
        profiles.push(...result.items);
        if (!result.items.length || profiles.length >= result.totalCount) break;
        page++;
      }
      const user = userId ? await directoryApi.get<UserItem>(`/api/users/${userId}`, contextId, signal) : null;
      const password = userId ? null : (await directoryApi.get<{ password: string | null }>("/api/users/initial-password", contextId, signal)).password;
      return { user, profiles, password };
    },
    retry: false, refetchOnWindowFocus: false, refetchOnReconnect: false,
  });
  const user = query.data?.user;
  async function run(action: () => Promise<void>) {
    if (busy) return;
    setBusy(true); setError(""); setNotice("");
    try { await action(); } catch (e) { setError(directoryError(e)); } finally { setBusy(false); }
  }
  async function save(path: string, method: "POST" | "PUT", body: unknown) {
    await directoryApi.send(path, method, body, contextId);
    await cache.invalidateQueries({ queryKey: ["users", tenantId, contextId] });
    setReset(false); setConfirmActive(false);
    setNotice("Alteração salva.");
    await query.refetch();
  }
  const title = userId ? "Editar usuário" : "Novo usuário";
  return <Stack spacing={3}>
    <LoadingState mode="blocking" open={busy || query.isPending || (manual && query.isFetching)} message={busy ? "Processando cadastro..." : "Carregando cadastro..."} />
    <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}>
      <FinancePageTitle navigationHref="/users">{title}</FinancePageTitle>
      {user && <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: "wrap" }}>
        <Button variant="outlined" color={user.active ? "error" : "success"}
          startIcon={user.active ? <BlockOutlinedIcon /> : <PersonAddAltOutlinedIcon />}
          onClick={() => { setError(""); setConfirmActive(true); }}>{user.active ? "Desativar usuário" : "Ativar usuário"}</Button>
        {user.id !== currentUserId && <Button variant="outlined" color="warning" startIcon={<LockResetOutlinedIcon />}
          onClick={() => { setError(""); setReset(true); }}>Redefinir senha</Button>}
      </Stack>}
    </Stack>

    {notice && <Alert severity="success">{notice}</Alert>}
    {query.isError && <Alert severity="error" action={<Button onClick={() => { setManual(true); void query.refetch().finally(() => setManual(false)); }}>Tentar novamente</Button>}>Não foi possível carregar o cadastro.</Alert>}
    {query.data && <PrototypePanel title="Dados do usuário">
      <UserEditor key={`${contextId}:${userId ?? "new"}`} user={query.data.user} profiles={query.data.profiles}
        initialPassword={query.data.password} busy={busy} error={reset || confirmActive ? "" : error}
        onClose={() => router.push("/users")} onSave={(input) => void run(async () => {
          if (userId) await save(`/api/users/${userId}`, "PUT", { name: input.name, email: input.email, profileId: input.profileId });
          else {
            const created = await directoryApi.send<UserItem>("/api/users", "POST", input, contextId);
            await cache.invalidateQueries({ queryKey: ["users", tenantId, contextId] });
            router.push(`/users/${created.id}`);
          }
        })} />
    </PrototypePanel>}

    {reset && user && <UserPasswordDialog name={user.name} busy={busy} error={error} onClose={() => { setReset(false); setError(""); }}
      onSave={() => void run(() => save(`/api/users/${user.id}/password`, "POST", { confirmGlobalImpact: true }))} />}
    {confirmActive && user && <Dialog open onClose={busy ? undefined : () => setConfirmActive(false)}>
      <DialogTitle>Confirmar alteração</DialogTitle>
      <DialogContent><Typography>{user.active ? "Desativar este usuário em todas as empresas? Todas as sessões serão encerradas." : "Reativar este usuário globalmente? Sessões anteriores permanecerão encerradas."}</Typography>
        {error && <Alert severity="error">{error}</Alert>}</DialogContent>
      <DialogActions><Button variant="outlined" disabled={busy} onClick={() => { setConfirmActive(false); setError(""); }}>Cancelar</Button>
        <Button variant="contained" disabled={busy} onClick={() => void run(() => save(`/api/users/${user.id}/active`, "PUT", { active: !user.active, confirmGlobalImpact: true }))}>Confirmar</Button></DialogActions>
    </Dialog>}
  </Stack>;
}
