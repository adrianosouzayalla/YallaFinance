import type { ReactNode } from "react";
import { Box, ButtonBase, Paper, Stack, Typography, alpha } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

interface SummaryCardProps {
  label: string;
  value: string;
  icon?: ReactNode;
  color?: "warning" | "info" | "success" | "error" | "default";
  onClick?: () => void;
  selected?: boolean;
}

export function SummaryCard({
  label,
  value,
  icon,
  color,
  onClick,
  selected = false,
}: SummaryCardProps) {
  const active = Boolean(onClick && selected);
  const neutral = color === "default";
  const statusColor = color && color !== "default" ? color : undefined;
  return (
    <Paper
      component={onClick ? ButtonBase : "div"}
      type={onClick ? "button" : undefined}
      onClick={onClick}
      aria-label={onClick ? `${label}: ${value}` : undefined}
      aria-pressed={onClick ? selected : undefined}
      variant="outlined"
      sx={{
        borderColor: statusColor ? `${statusColor}.main` : financeTokens.colors.subtleBorder,
        borderWidth: active ? 2 : 1,
        bgcolor: color
          ? (theme) =>
              alpha(
                statusColor ? theme.palette[statusColor].main : theme.palette.text.primary,
                active ? 0.12 : 0.04,
              )
          : undefined,
        borderRadius: `${financeTokens.radius.panel}px`,
        minHeight: financeTokens.density.summaryCardMinHeight,
        p: financeTokens.density.summaryCardPadding,
        ...(onClick && {
          display: "block",
          width: "100%",
          textAlign: "left",
          "&:hover": {
            bgcolor: color
              ? (theme) =>
                  alpha(
                    statusColor ? theme.palette[statusColor].main : theme.palette.text.primary,
                    0.08,
                  )
              : "action.hover",
          },
          "&.Mui-focusVisible": {
            outline: "2px solid",
            outlineColor: statusColor
              ? `${statusColor}.main`
              : color
                ? "text.secondary"
                : financeTokens.colors.header,
            outlineOffset: 3,
          },
        }),
      }}
    >
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", gap: 2 }}>
        <Stack spacing={0.75}>
          <Typography
            color="text.secondary"
            sx={{ fontSize: financeTokens.typography.secondary, fontWeight: 500 }}
          >
            {label}
          </Typography>
          <Typography
            sx={{
              color: color
                ? neutral
                  ? "text.secondary"
                  : `${color}.main`
                : financeTokens.colors.header,
              fontSize: financeTokens.typography.summaryMetric,
              fontWeight: 700,
            }}
          >
            {value}
          </Typography>
        </Stack>
        {icon && (
          <Box
            aria-hidden="true"
            sx={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: financeTokens.density.summaryIconSize,
              height: financeTokens.density.summaryIconSize,
              flexShrink: 0,
              borderRadius: 2,
              color: color
                ? neutral
                  ? "text.secondary"
                  : `${color}.main`
                : financeTokens.colors.header,
              bgcolor: color
                ? (theme) =>
                    alpha(
                      statusColor ? theme.palette[statusColor].main : theme.palette.text.primary,
                      0.12,
                    )
                : financeTokens.colors.stepBackground,
            }}
          >
            {icon}
          </Box>
        )}
      </Stack>
    </Paper>
  );
}
