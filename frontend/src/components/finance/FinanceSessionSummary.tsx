"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, Avatar, Box, IconButton, ListItemIcon, ListItemText, Menu, MenuItem, Typography } from "@mui/material";
import { LoadingState } from "@/components/ui/LoadingState";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";
import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import { logoutAction } from "@/features/auth/actions";
import type { TenantOption } from "@/features/auth/authSession";
import { financeTokens } from "@/theme/designTokens";
import { clearBrowserSessionData } from "@/features/auth/browser-session";
import { directoryApi, directoryError } from "@/features/users/directory.client";
import { OwnPasswordDialog } from "@/features/users/components/OwnPasswordDialog";

export type FinanceSession = {
  displayName?: string;
  role?: string;
  organization?: string;
  environment?: string;
  avatarUrl?: string;
  canLogout: boolean;
  contextId?: string;
  tenantId?: string;
  tenants?: TenantOption[];
  functionalityCodes?: string[];
};
function initials(displayName?: string): string {
  const words = displayName?.trim().split(/\s+/).filter(Boolean) ?? [];
  return words.slice(0, 2).map((word) => word[0]?.toUpperCase()).join("") || "U";
}

export function FinanceSessionSummary({ session }: { session?: FinanceSession }) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [pendingMessage, setPendingMessage] = useState("Encerrando sessão...");
  const [error, setError] = useState("");
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [passwordDialogOpen, setPasswordDialogOpen] = useState(false);
  if (!session?.displayName && !session?.role && !session?.organization && !session?.environment) return null;

  async function changeOwnPassword(currentPassword: string, newPassword: string) {
    if (pending) return;
    if (!session?.contextId) { setError("Sessão indisponível. Entre novamente."); return; }
    setPending(true);
    setPendingMessage("Alterando senha...");
    setError("");
    try {
      await directoryApi.send("/api/auth/password", "POST", { currentPassword, newPassword }, session.contextId);
      clearBrowserSessionData();
      router.replace("/login");
      router.refresh();
    } catch (cause) {
      setError(directoryError(cause));
    } finally {
      setPending(false);
    }
  }

  const sessionActions = [
    { id: "change-password", label: "Alterar senha", icon: <LockResetOutlinedIcon fontSize="small" />,
      onSelect: () => { setError(""); setPasswordDialogOpen(true); } },
  ];
  const organizationLabel = [session.organization, session.environment].filter(Boolean).join(" • ");

  return (
    <Box sx={{ borderTop: `1px solid ${financeTokens.colors.subtleBorder}`, flexShrink: 0, p: 2 }}>
      <LoadingState mode="blocking" open={pending} message={pendingMessage} />
      {error && <Alert severity="error">{error}</Alert>}
      <Box sx={{ display: "grid", gap: 0.5, minWidth: 0 }}>
        <Box role="group" aria-label="Dados do usuário" sx={{ alignItems: "center", display: "flex", gap: 1.25, minWidth: 0 }}>
          <Avatar alt="Usuário" src={session.avatarUrl} sx={{ bgcolor: financeTokens.colors.header, height: 40, width: 40 }}>
            {initials(session.displayName)}
          </Avatar>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            {session.displayName && <Typography noWrap sx={{ color: financeTokens.colors.text, fontSize: financeTokens.typography.body, fontWeight: 700 }}>{session.displayName}</Typography>}
            {session.role && <Typography noWrap sx={{ color: financeTokens.colors.mutedText, fontSize: financeTokens.typography.secondary }}>{session.role}</Typography>}
            {organizationLabel && <Box sx={{ alignItems: "center", display: "flex", gap: 0.5, minWidth: 0, mt: 0.25 }}>
              <BusinessOutlinedIcon aria-hidden="true" sx={{ color: financeTokens.colors.mutedText, flexShrink: 0, fontSize: 14 }} />
              <Typography noWrap title={organizationLabel} sx={{ color: financeTokens.colors.mutedText, fontSize: financeTokens.typography.secondary, minWidth: 0 }}>{organizationLabel}</Typography>
            </Box>}
          </Box>
        </Box>
        <Box role="group" aria-label="Ações da sessão" sx={{ alignItems: "center", display: "flex", gap: 0.5, justifyContent: "flex-end", minWidth: 0 }}>
          <IconButton aria-label="Configurações da sessão" aria-haspopup="menu" aria-expanded={Boolean(menuAnchor)}
            color="inherit" size="small" disabled={pending} onClick={(event) => setMenuAnchor(event.currentTarget)}>
            <SettingsOutlinedIcon fontSize="small" />
          </IconButton>
          {session.canLogout && <IconButton aria-label="Sair" color="inherit" size="small" disabled={pending} onClick={async () => {
            setPending(true); setPendingMessage("Encerrando sessão..."); setError("");
            try {
              const result = await logoutAction();
              if (result?.error) setError(result.error);
              else clearBrowserSessionData();
            } catch { setError("Não foi possível encerrar a sessão. Tente novamente."); }
            finally { setPending(false); }
          }}>
            <LogoutOutlinedIcon fontSize="small" />
          </IconButton>}
        </Box>
      </Box>
      <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={() => setMenuAnchor(null)}>
        {sessionActions.map((action) => <MenuItem key={action.id} onClick={() => { setMenuAnchor(null); action.onSelect(); }}>
          <ListItemIcon>{action.icon}</ListItemIcon><ListItemText>{action.label}</ListItemText>
        </MenuItem>)}
      </Menu>
      {passwordDialogOpen && <OwnPasswordDialog busy={pending} error={error}
        onClose={() => { setPasswordDialogOpen(false); setError(""); }}
        onSave={(currentPassword, newPassword) => void changeOwnPassword(currentPassword, newPassword)} />}
    </Box>
  );
}
