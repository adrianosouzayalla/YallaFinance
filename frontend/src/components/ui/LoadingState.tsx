import { Modal, Stack, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { YallaCamelLoader } from "./YallaCamelLoader";

interface LoadingStateProps {
  mode: "page" | "blocking";
  open?: boolean;
  message?: string;
}

const defaultMessage = "Carregando...";

export function LoadingState({ mode, open = true, message = defaultMessage }: LoadingStateProps) {
  if (mode === "blocking") {
    return (
      <Modal
        open={open}
        aria-labelledby="yalla-loading-message"
        slotProps={{ backdrop: { sx: { backgroundColor: financeTokens.overlays.loadingScreen } } }}
        sx={{ zIndex: (theme) => theme.zIndex.modal + 1 }}
      >
        <Stack
          role="dialog"
          aria-modal="true"
          aria-labelledby="yalla-loading-message"
          tabIndex={-1}
          sx={{
            alignItems: "center",
            color: financeTokens.colors.header,
            gap: 1.5,
            left: "50%",
            outline: 0,
            position: "absolute",
            top: "50%",
            transform: "translate(-50%, -50%)",
          }}
        >
          <span aria-hidden="true">
            <YallaCamelLoader size={36} />
          </span>
          <Typography
            id="yalla-loading-message"
            role="status"
            aria-label={message}
            aria-live="polite"
            sx={{ fontWeight: 600 }}
          >
            {message}
          </Typography>
        </Stack>
      </Modal>
    );
  }

  return (
    <Stack
      role="status"
      aria-label={message}
      aria-live="polite"
      sx={{
        alignItems: "center",
        backgroundColor: financeTokens.colors.canvas,
        color: financeTokens.colors.header,
        gap: 2,
        justifyContent: "center",
        minHeight: financeTokens.shell.viewportHeight,
        width: "100%",
      }}
    >
      <Typography sx={{ fontSize: financeTokens.typography.metric, fontWeight: 700 }}>
        Yalla Finance
      </Typography>
      <span aria-hidden="true">
        <YallaCamelLoader size={36} />
      </span>
      <Typography color="text.secondary" variant="body2">
        {message}
      </Typography>
    </Stack>
  );
}
