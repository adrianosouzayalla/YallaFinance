"use client";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from "@mui/material";
import ReplayIcon from "@mui/icons-material/Replay";

export function JobReprocessDialog({
  open,
  pending,
  onClose,
  onConfirm,
}: {
  open: boolean;
  pending: boolean;
  onClose(): void;
  onConfirm(): void;
}) {
  return (
    <Dialog
      open={open}
      onClose={() => {
        if (!pending) onClose();
      }}
      aria-labelledby="job-reprocess-title"
      aria-describedby="job-reprocess-description"
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle id="job-reprocess-title">Reprocessar Job</DialogTitle>
      <DialogContent>
        <DialogContentText id="job-reprocess-description">
          Enviar este Job para uma nova tentativa? O histórico das execuções anteriores será
          mantido.
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button disabled={pending} onClick={onClose}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          loading={pending}
          disabled={pending}
          startIcon={<ReplayIcon />}
          onClick={onConfirm}
        >
          Reprocessar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
