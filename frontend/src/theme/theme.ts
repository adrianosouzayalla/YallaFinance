import { createTheme } from "@mui/material/styles";
import { financeTokens } from "./designTokens";

export const theme = createTheme({
  breakpoints: {
    values: { xs: 0, sm: 480, md: 720, lg: 960, xl: 1229 },
  },
  palette: {
    mode: "light",
    primary: { main: financeTokens.colors.action, dark: financeTokens.colors.actionHover },
    secondary: { main: financeTokens.colors.header },
    success: { main: financeTokens.colors.paidText, light: financeTokens.colors.paidBackground },
    info: { main: financeTokens.colors.action, light: financeTokens.colors.activeNavigation },
    warning: {
      main: financeTokens.colors.chargingText,
      light: financeTokens.colors.chargingBackground,
    },
    error: { main: financeTokens.colors.openText, light: financeTokens.colors.openBackground },
    text: {
      primary: financeTokens.colors.text,
      secondary: financeTokens.colors.mutedText,
    },
    divider: financeTokens.colors.border,
    background: {
      default: financeTokens.colors.canvas,
      paper: financeTokens.colors.panel,
    },
  },
  typography: {
    fontFamily: "Inter, Arial, sans-serif",
    fontSize: 13,
    h1: { fontSize: financeTokens.typography.pageTitle, fontWeight: 700 },
    h2: { fontSize: financeTokens.typography.sectionTitle, fontWeight: 700 },
    h3: { fontSize: financeTokens.typography.sectionTitle, fontWeight: 600 },
    h4: { fontSize: financeTokens.typography.sectionTitle, fontWeight: 600 },
    h5: { fontSize: financeTokens.typography.body, fontWeight: 600 },
    h6: { fontSize: financeTokens.typography.secondary, fontWeight: 600 },
    body1: { fontSize: financeTokens.typography.body, lineHeight: 1.5 },
    body2: { fontSize: financeTokens.typography.secondary, lineHeight: 1.5 },
  },
  shape: {
    borderRadius: financeTokens.radius.control,
  },
  spacing: 2.88,
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: { backgroundColor: financeTokens.colors.canvas, color: financeTokens.colors.text },
        ':where(a, button, input, select, textarea, [role="button"]):focus-visible': {
          outline: `3px solid ${financeTokens.colors.action}`,
          outlineOffset: 2,
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          minHeight: 36,
          borderRadius: financeTokens.radius.control,
          textTransform: "none",
          fontSize: financeTokens.typography.body,
          fontWeight: 600,
        },
      },
    },
    MuiInputBase: {
      styleOverrides: {
        root: {
          "&.Mui-disabled": {
            backgroundColor: financeTokens.colors.sidebar,
          },
        },
        input: { fontSize: financeTokens.typography.body },
      },
    },
    MuiInputLabel: {
      styleOverrides: {
        root: { fontSize: financeTokens.typography.body },
      },
    },
    MuiFormHelperText: {
      styleOverrides: {
        root: { fontSize: financeTokens.typography.secondary },
      },
    },
    MuiTextField: {
      defaultProps: {
        size: "small",
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          fontSize: financeTokens.typography.body,
          "&.MuiTableCell-sizeSmall": {
            padding: `${financeTokens.density.tableCellVerticalPadding} ${financeTokens.density.tableCellHorizontalPadding}`,
          },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: financeTokens.radius.panel,
          boxShadow: "none",
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: financeTokens.radius.control,
        },
      },
    },
  },
});
