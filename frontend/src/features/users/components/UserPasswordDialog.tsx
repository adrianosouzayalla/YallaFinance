"use client";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
} from "@mui/material";

export function UserPasswordDialog({
  name,
  error,
  busy,
  onClose,
  onSave,
}: {
  name: string;
  error: string;
  busy: boolean;
  onClose: () => void;
  onSave: () => void;
}) {
  return (
    <Dialog open fullWidth maxWidth="sm" onClose={busy ? undefined : onClose}>
      <DialogTitle>Redefinir senha de {name}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Alert severity="warning">
            A senha padrão desta empresa será aplicada. A alteração vale em todas as empresas e
            encerra todas as sessões desse usuário.
          </Alert>
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="outlined" disabled={busy} onClick={onClose}>
          Cancelar
        </Button>
        <Button disabled={busy} onClick={onSave} variant="contained">
          Confirmar redefinição
        </Button>
      </DialogActions>
    </Dialog>
  );
}
