import type { ReactNode } from "react";
import { Box, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { buttonRow, fieldGap } from "@/theme/layout";

export function renderDirectChargeFooter(actions: ReactNode, caption = "* Campos obrigatórios.") {
  return (
    <Box
      sx={{
        display: "flex",
        alignItems: "center",
        flexWrap: "wrap",
        gap: fieldGap,
        gridColumn: "1 / -1",
        justifyContent: "space-between",
      }}
    >
      <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
        {caption}
      </Typography>
      <Box sx={buttonRow}>{actions}</Box>
    </Box>
  );
}
