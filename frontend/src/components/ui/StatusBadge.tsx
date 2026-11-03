import { Box } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

const tones = {
  default: { backgroundColor: financeTokens.colors.sidebar, color: financeTokens.colors.mutedText },
  info: { backgroundColor: "#E3F2FD", color: "#0D47A1" },
  warning: { backgroundColor: "#FFF8E1", color: "#6D4C00" },
  orange: { backgroundColor: "#FFF3E0", color: "#A33A00" },
  success: { backgroundColor: "#E8F5E9", color: "#1B5E20" },
  error: { backgroundColor: "#FDE9DF", color: "#C94A3D" },
} as const;

export type StatusBadgeTone = keyof typeof tones;

export function StatusBadge({ label, tone = "default" }: { label: string; tone?: StatusBadgeTone }) {
  return (
    <Box component="span" sx={{
      ...tones[tone],
      borderRadius: `${financeTokens.radius.badge}px`,
      display: "inline-flex",
      fontSize: financeTokens.typography.secondary,
      fontWeight: 600,
      lineHeight: 1,
      px: 1.25,
      py: 0.75,
      whiteSpace: "nowrap",
    }}>
      {label}
    </Box>
  );
}
