"use client";

import { useState } from "react";
import { Alert, Button, Paper, Stack, Typography } from "@mui/material";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";
import { LoadingState } from "@/components/ui/LoadingState";
import { logoutAction } from "@/features/auth/actions";
import { clearBrowserSessionData } from "@/features/auth/browser-session";

export function HomeFallback() {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  async function logout() {
    if (pending) return;
    setPending(true);
    setError("");
    try {
      const result = await logoutAction();
      if (result?.error) setError(result.error);
      else clearBrowserSessionData();
    } catch {
      setError("Não foi possível encerrar a sessão. Tente novamente.");
    } finally {
      setPending(false);
    }
  }
  return <>
    <LoadingState mode="blocking" open={pending} message="Encerrando sessão..." />
    <Paper variant="outlined" sx={{ mx: "auto", maxWidth: 680, p: { xs: 3, md: 5 }, textAlign: "center" }}>
      <Stack spacing={2} sx={{ alignItems: "center" }}>
        <Typography component="h1" variant="h4">Bem-vindo ao Yalla Finance</Typography>
        <Typography color="text.secondary">
          A página inicial está disponível para todos. Os indicadores do painel exigem permissão de consulta a débitos.
        </Typography>
        {error && <Alert severity="error">{error}</Alert>}
        <Button disabled={pending} onClick={() => void logout()} startIcon={<LogoutOutlinedIcon />} variant="outlined">Sair da sessão</Button>
      </Stack>
    </Paper>
  </>;
}
