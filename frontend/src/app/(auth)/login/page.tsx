import { Box } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { LoginForm } from "@/features/auth/components/LoginForm";

export default function LoginPage() {
  return (
    <Box
      component="main"
      sx={{
        display: "grid",
        gridTemplateColumns: {
          xs: "1fr",
          md: financeTokens.auth.mediumPanelColumns,
          lg: financeTokens.auth.widePanelColumns,
        },
        minHeight: financeTokens.shell.viewportHeight,
        backgroundColor: financeTokens.colors.canvas,
      }}
    >
      <Box
        sx={{
          display: { xs: "none", md: "block" },
          minHeight: financeTokens.shell.viewportHeight,
          overflow: "hidden",
          position: "relative",
          backgroundColor: financeTokens.colors.header,
          "&::after": {
            background: financeTokens.overlays.loginAccent,
            content: '""',
            inset: 0,
            pointerEvents: "none",
            position: "absolute",
          },
        }}
      >
        <Box
          component="img"
          src="/assets/brand/imagem_login_v2.png"
          alt="Operação financeira e frota Yalla"
          sx={{
            height: "100%",
            inset: 0,
            objectFit: "cover",
            objectPosition: { md: "center 38%", xl: "center 34%" },
            position: "absolute",
            width: "100%",
          }}
        />
      </Box>
      <Box
        sx={{
          alignItems: "center",
          backgroundColor: financeTokens.colors.authBackground,
          boxShadow: financeTokens.shadows.loginPanel,
          display: "flex",
          justifyContent: "center",
          minHeight: financeTokens.shell.viewportHeight,
          px: financeTokens.auth.panelPaddingX,
          py: financeTokens.auth.panelPaddingY,
          position: "relative",
          zIndex: 1,
        }}
      >
        <LoginForm />
      </Box>
    </Box>
  );
}
