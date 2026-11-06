import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import LockRoundedIcon from "@mui/icons-material/LockRounded";
import { LoadingState } from "@/components/ui/LoadingState";
import { financeTokens } from "@/theme/designTokens";
import { checkoutColors } from "@/theme/checkoutTheme";

export function PageFrame({ children }: { children: React.ReactNode }) {
  return (
    <Box
      sx={{
        minHeight: financeTokens.shell.viewportHeight,
        bgcolor: checkoutColors.pageBackground,
        backgroundImage: checkoutColors.pageGradient,
        px: { xs: 1.5, sm: 2.5, md: 4 },
        pt: { xs: 2, md: 4 },
        pb: 2,
        overflowX: "hidden",
      }}
    >
      <Box sx={{ maxWidth: 1120, mx: "auto", minWidth: 0 }}>{children}</Box>
    </Box>
  );
}

export function Brand() {
  return (
    <Stack
      component="header"
      direction="row"
      sx={{
        justifyContent: "space-between",
        alignItems: "center",
        flexWrap: "wrap",
        gap: { xs: 1, sm: 1.5 },
        mb: { xs: 3, md: 4 },
      }}
    >
      <Box
        component="img"
        src="/assets/brand/yallafinance-logo.svg"
        alt="Yalla Finance"
        sx={{
          display: "block",
          width: { xs: 150, sm: 190, md: 216 },
          maxWidth: "100%",
          height: "auto",
        }}
      />
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: "center", ml: "auto", flexShrink: 0, color: checkoutColors.muted }}
      >
        <LockRoundedIcon aria-hidden="true" sx={{ fontSize: { xs: 16, md: 20 } }} />
        <Typography sx={{ fontSize: financeTokens.typography.secondary, whiteSpace: "nowrap" }}>
          Pagamento seguro
        </Typography>
      </Stack>
    </Stack>
  );
}

export function CheckoutUnavailableView({
  temporaryError,
  isRefreshing,
  onRetry,
}: {
  temporaryError: boolean;
  isRefreshing: boolean;
  onRetry: () => void;
}) {
  const message = temporaryError
    ? "Não conseguimos abrir sua cobrança agora. Tente novamente em instantes."
    : "Este link de pagamento não está disponível. Entre em contato com quem enviou a cobrança.";
  return (
    <PageFrame>
      <Brand />
      <Alert
        severity={temporaryError ? "error" : "warning"}
        role="alert"
        action={
          temporaryError ? (
            <Button color="inherit" disabled={isRefreshing} onClick={onRetry}>
              {isRefreshing ? "Tentando…" : "Tentar novamente"}
            </Button>
          ) : undefined
        }
      >
        {message}
      </Alert>
      <LoadingState mode="blocking" open={isRefreshing} message="Atualizando o checkout..." />
    </PageFrame>
  );
}

export function CheckoutExpiredView() {
  return (
    <PageFrame>
      <Box component="main" sx={{ minHeight: "70vh", display: "grid", placeItems: "center" }}>
        <Typography
          role="alert"
          sx={{
            color: checkoutColors.muted,
            fontSize: financeTokens.typography.checkoutBody,
            fontWeight: 600,
            textAlign: "center",
          }}
        >
          Este link de pagamento expirou. Peça um novo link a quem enviou a cobrança.
        </Typography>
      </Box>
    </PageFrame>
  );
}
