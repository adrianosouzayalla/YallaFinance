import { Box } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

const tones = {
  default: { backgroundColor: financeTokens.colors.sidebar, color: financeTokens.colors.mutedText },
  info: {
    backgroundColor: financeTokens.colors.infoBackground,
    color: financeTokens.colors.infoText,
  },
  warning: {
    backgroundColor: financeTokens.colors.warningBackground,
    color: financeTokens.colors.warningText,
  },
  orange: {
    backgroundColor: financeTokens.colors.orangeBackground,
    color: financeTokens.colors.orangeText,
  },
  success: {
    backgroundColor: financeTokens.colors.successBackground,
    color: financeTokens.colors.successText,
  },
  error: {
    backgroundColor: financeTokens.colors.openBackground,
    color: financeTokens.colors.openText,
  },
} as const;

export type StatusBadgeTone = keyof typeof tones;

export function StatusBadge({
  label,
  tone = "default",
}: {
  label: string;
  tone?: StatusBadgeTone;
}) {
  return (
    <Box
      component="span"
      sx={{
        ...tones[tone],
        borderRadius: `${financeTokens.radius.badge}px`,
        display: "inline-flex",
        fontSize: financeTokens.typography.secondary,
        fontWeight: 600,
        lineHeight: 1,
        px: 1.25,
        py: 0.75,
        whiteSpace: "nowrap",
      }}
    >
      {label}
    </Box>
  );
}
