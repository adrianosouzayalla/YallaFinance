import { checkoutColors } from "./checkoutTheme";
import { financeTokens } from "./designTokens";

export const panelStyle = {
  minWidth: 0,
  minHeight: { md: 500 },
  border: `1px solid ${checkoutColors.panelBorder}`,
  borderRadius: "12px",
  bgcolor: checkoutColors.panelBackground,
  boxShadow: checkoutColors.panelShadow,
} as const;

export const sectionHeading = {
  color: checkoutColors.ink,
  fontSize: financeTokens.typography.checkoutSection,
  fontWeight: 720,
  letterSpacing: "-0.025em",
  lineHeight: 1.3,
} as const;

export const optionSx = {
  m: 0,
  minHeight: { xs: 84, md: 108 },
  border: `1px solid ${checkoutColors.divider}`,
  borderRadius: "14px",
  bgcolor: checkoutColors.white,
  alignItems: "center",
  transition: "border-color 120ms ease, background-color 120ms ease",
  "&:has(.Mui-checked)": {
    border: `2px solid ${checkoutColors.focus}`,
    bgcolor: checkoutColors.optionSelectedBackground,
  },
  "&:focus-within": { outline: `3px solid ${checkoutColors.focusSoft}`, outlineOffset: 2 },
  "& .MuiFormControlLabel-label": { flex: 1, minWidth: 0 },
} as const;

export const secondaryButtonSx = {
  minHeight: 48,
  borderRadius: "10px",
  borderColor: checkoutColors.secondaryBorder,
  color: checkoutColors.action,
  fontSize: financeTokens.typography.body,
  fontWeight: 700,
  textTransform: "none",
} as const;

export const primaryButtonSx = {
  pt: 1.2,
  pb: 1.2,
  minHeight: 52,
  borderRadius: "10px",
  bgcolor: checkoutColors.action,
  boxShadow: checkoutColors.actionShadow,
  fontSize: financeTokens.typography.body,
  fontWeight: 700,
  textTransform: "none",
  "&:hover": { bgcolor: checkoutColors.actionHover, boxShadow: checkoutColors.actionShadowHover },
  "&:focus-visible": { outline: `3px solid ${checkoutColors.focus}`, outlineOffset: 3 },
  "&.Mui-disabled": {
    color: checkoutColors.white,
    bgcolor: checkoutColors.action,
    opacity: 0.72,
    boxShadow: "none",
    cursor: "not-allowed",
  },
} as const;
