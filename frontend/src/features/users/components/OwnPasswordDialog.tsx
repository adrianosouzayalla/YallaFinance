"use client";
import { useState } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from "@mui/material";

export function OwnPasswordDialog({
  error,
  busy,
  onClose,
  onSave,
}: {
  error: string;
  busy: boolean;
  onClose: () => void;
  onSave: (currentPassword: string, newPassword: string) => void;
}) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const valid =
    currentPassword.length > 0 &&
    newPassword.length >= 12 &&
    newPassword.length <= 128 &&
    newPassword === confirmation;
  return (
    <Dialog open fullWidth maxWidth="sm" onClose={busy ? undefined : onClose}>
      <DialogTitle>Alterar minha senha</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Alert severity="warning">
            A alteração encerra todas as suas sessões. Será necessário entrar novamente.
          </Alert>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            size="small"
            fullWidth
            label="Senha atual"
            type="password"
            autoComplete="current-password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
          <TextField
            size="small"
            fullWidth
            label="Nova senha"
            type="password"
            autoComplete="new-password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            helperText="De 12 a 128 caracteres."
          />
          <TextField
            size="small"
            fullWidth
            label="Confirmar nova senha"
            type="password"
            autoComplete="new-password"
            value={confirmation}
            onChange={(e) => setConfirmation(e.target.value)}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="outlined" disabled={busy} onClick={onClose}>
          Cancelar
        </Button>
        <Button
          disabled={busy || !valid}
          onClick={() => onSave(currentPassword, newPassword)}
          variant="contained"
        >
          Alterar senha e sair
        </Button>
      </DialogActions>
    </Dialog>
  );
}
