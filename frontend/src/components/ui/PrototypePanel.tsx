import type { PropsWithChildren, ReactNode } from "react";
import { Box, Paper, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

interface PrototypePanelProps extends PropsWithChildren {
  action?: ReactNode;
  borderStyle?: "solid" | "dashed";
  highlighted?: boolean;
  highlightedBackground?: string;
  headerPaddingX?: string;
  title: string;
}

export function PrototypePanel({
  action,
  borderStyle = "solid",
  children,
  highlighted = false,
  highlightedBackground = financeTokens.colors.tableHeader,
  headerPaddingX = `${financeTokens.density.panelHeaderHorizontalPadding}px`,
  title,
}: PrototypePanelProps) {
  return (
    <Paper
      component="section"
      variant="outlined"
      sx={{
        borderColor: financeTokens.colors.subtleBorder,
        borderStyle,
        borderRadius: `${financeTokens.radius.panel}px`,
        backgroundColor: highlighted ? highlightedBackground : undefined,
        overflow: "hidden",
      }}
    >
      <Box
        sx={{
          alignItems: "center",
          backgroundColor: highlighted ? highlightedBackground : undefined,
          borderBottom: `1px solid ${financeTokens.colors.subtleBorder}`,
          display: "flex",
          justifyContent: "space-between",
          minHeight: financeTokens.density.panelHeaderHeight,
          px: headerPaddingX,
          py: `${financeTokens.density.panelHeaderVerticalPadding}px`,
        }}
      >
        <Typography
          component="h2"
          variant="h6"
          sx={highlighted ? { color: financeTokens.colors.header, fontWeight: 700 } : undefined}
        >
          {title}
        </Typography>
        {action}
      </Box>
      {children}
    </Paper>
  );
}
