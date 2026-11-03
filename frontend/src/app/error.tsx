"use client";

import { Alert, Button, Paper, Stack, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

export default function Error({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <Stack
      sx={{
        alignItems: "center",
        backgroundColor: financeTokens.colors.canvas,
        justifyContent: "center",
        minHeight: financeTokens.shell.viewportHeight,
        p: "clamp(16px, 4vw, 32px)",
      }}
    >
      <Paper
        sx={{
          border: `1px solid ${financeTokens.colors.subtleBorder}`,
          maxWidth: 416,
          p: "clamp(12px, 3.2vw, 24px)",
          width: "100%",
        }}
      >
        <Stack sx={{ alignItems: "center", gap: 1.5 }}>
          <Typography
            component="h1"
            sx={{ color: financeTokens.colors.header, fontSize: financeTokens.typography.pageTitle, fontWeight: 700 }}
          >
            Yalla Finance
          </Typography>
          <Alert severity="error" sx={{ width: "100%" }}>
            Não foi possível carregar esta página.
          </Alert>
          <Button variant="contained" onClick={() => reset()}>
            Tentar novamente
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
}
