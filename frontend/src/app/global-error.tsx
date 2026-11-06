"use client";

import { Button, Paper, Stack, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

export default function GlobalError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <html lang="pt-BR">
      <body style={{ margin: 0, width: "100%", minHeight: "100dvh", overflowX: "hidden" }}>
        <div
          style={{
            zoom: financeTokens.uiScale,
            width: "125vw",
            minHeight: financeTokens.shell.viewportHeight,
          }}
        >
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
                  sx={{
                    color: financeTokens.colors.header,
                    fontSize: financeTokens.typography.pageTitle,
                    fontWeight: 700,
                  }}
                >
                  Yalla Finance
                </Typography>
                <Typography color="text.primary" component="p" sx={{ textAlign: "center" }}>
                  Ocorreu um erro inesperado.
                </Typography>
                <Button variant="contained" onClick={() => reset()}>
                  Recarregar
                </Button>
              </Stack>
            </Paper>
          </Stack>
        </div>
      </body>
    </html>
  );
}
