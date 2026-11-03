"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from "@mui/material";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import PersonAddAltOutlinedIcon from "@mui/icons-material/PersonAddAltOutlined";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { LoadingState } from "@/components/ui/LoadingState";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useToast } from "@/providers/ToastProvider";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { directoryApi, directoryError } from "@/features/users/directory.client";
import type { FunctionalityOption, ProfileItem } from "@/features/users/types";
import { ProfileEditor } from "./ProfileEditor";

export function ProfileFormPage({ profileId }: { profileId?: string }) {
  const { tenantId, contextId } = useSessionContext();
  const router = useRouter();
  const cache = useQueryClient();
  const { showToast } = useToast();
  const [busy, setBusy] = useState(false);
  const [manual, setManual] = useState(false);
  const [confirmActive, setConfirmActive] = useState(false);
  const query = useQuery({
    queryKey: ["profile-form", tenantId, contextId, profileId],
    queryFn: ({ signal }) => profileId
      ? directoryApi.get<ProfileItem>(`/api/access-profiles/${profileId}`, contextId, signal)
      : Promise.resolve(null),
    retry: false, refetchOnWindowFocus: false, refetchOnReconnect: false,
  });
  const functionalitiesQuery = useQuery({
    queryKey: ["access-profile-functionalities", tenantId, contextId],
    queryFn: ({ signal }) => directoryApi.get<FunctionalityOption[]>("/api/access-profiles/functionalities", contextId, signal),
    retry: false, refetchOnWindowFocus: false, refetchOnReconnect: false,
  });
  useEffect(() => {
    if (query.isError) showToast({ message: "Não foi possível carregar o perfil.", severity: "error" });
  }, [query.isError, showToast]);
  useEffect(() => {
    if (functionalitiesQuery.isError) showToast({ message: "Não foi possível carregar as funcionalidades.", severity: "error" });
  }, [functionalitiesQuery.isError, showToast]);
  if (manual && !query.isFetching) setManual(false);
  const profile = query.data;

  async function run(action: () => Promise<void>) {
    if (busy) return;
    setBusy(true);
    try { await action(); }
    catch (cause) { showToast({ message: directoryError(cause), severity: "error" }); }
    finally { setBusy(false); }
  }

  async function save(input: { code: string; name: string; active: boolean; functionalityIds: string[] }) {
    if (profileId) {
      await directoryApi.send(`/api/access-profiles/${profileId}`, "PUT", { name: input.name, functionalityIds: input.functionalityIds }, contextId);
    } else {
      await directoryApi.send<ProfileItem>("/api/access-profiles", "POST", input, contextId);
    }
    await cache.invalidateQueries({ queryKey: ["profiles", tenantId, contextId] });
    await cache.invalidateQueries({ queryKey: ["access-profile-functionalities", tenantId, contextId] });
    showToast({ message: "Perfil salvo.", severity: "success" });
    router.push("/profiles");
  }

  async function changeActive() {
    if (!profileId || !profile) return;
    await directoryApi.send(`/api/access-profiles/${profileId}/active`, "PUT", { active: !profile.active }, contextId);
    await cache.invalidateQueries({ queryKey: ["profiles", tenantId, contextId] });
    setConfirmActive(false);
    showToast({ message: profile.active ? "Perfil desativado." : "Perfil ativado.", severity: "success" });
    await query.refetch();
  }

  const title = profileId ? "Editar perfil" : "Novo perfil";
  return <Stack spacing={3}>
    <LoadingState mode="blocking" open={busy || query.isPending || functionalitiesQuery.isPending || (manual && query.isFetching)}
      message={busy ? "Salvando perfil..." : "Carregando perfil..."} />
    <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 2 }}>
      <FinancePageTitle navigationHref="/profiles">{title}</FinancePageTitle>
      {profile && <Button variant="outlined" color={profile.active ? "error" : "success"}
        startIcon={profile.active ? <BlockOutlinedIcon /> : <PersonAddAltOutlinedIcon />}
        onClick={() => setConfirmActive(true)}>
        {profile.active ? "Desativar perfil" : "Ativar perfil"}
      </Button>}
    </Stack>
    {query.isError && <Button variant="outlined" onClick={() => { setManual(true); void query.refetch(); }}>Tentar carregar o perfil novamente</Button>}
    {functionalitiesQuery.isError && <Button variant="outlined" onClick={() => void functionalitiesQuery.refetch()}>Tentar carregar as funcionalidades novamente</Button>}
    {query.data !== undefined && functionalitiesQuery.data !== undefined && <PrototypePanel title="Dados do perfil">
      <ProfileEditor key={`${contextId}:${profileId ?? "new"}`} profile={profile ?? null} functionalities={functionalitiesQuery.data}
        busy={busy} onClose={() => router.push("/profiles")} onSave={(input) => void run(() => save(input))} />
    </PrototypePanel>}
    {confirmActive && profile && <Dialog open onClose={busy ? undefined : () => setConfirmActive(false)}>
      <DialogTitle>{profile.active ? "Desativar perfil" : "Ativar perfil"}</DialogTitle>
      <DialogContent>
        <Typography>Confirmar {profile.active ? "a desativação" : "a ativação"} do perfil {profile.name}?</Typography>
      </DialogContent>
      <DialogActions>
        <Button variant="outlined" disabled={busy} onClick={() => setConfirmActive(false)}>Cancelar</Button>
        <Button variant="contained" color={profile.active ? "error" : "success"} disabled={busy}
          onClick={() => void run(changeActive)}>Confirmar</Button>
      </DialogActions>
    </Dialog>}
  </Stack>;
}
