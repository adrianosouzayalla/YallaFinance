import { Alert, Box, Button, Stack, SvgIcon, Typography } from "@mui/material";
import PixIcon from "@mui/icons-material/Pix";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import CreditCardRoundedIcon from "@mui/icons-material/CreditCardRounded";
import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import type { SvgIconProps } from "@mui/material/SvgIcon";
import type { StatusAlertSpec } from "./checkoutFlow";
import { financeTokens } from "@/theme/designTokens";
import { checkoutColors } from "@/theme/checkoutTheme";
import { secondaryButtonSx, primaryButtonSx } from "@/theme/checkoutStyles";
import type { PublicPaymentMethod } from "./checkoutTypes";

export function StatusAlert({ severity, role, message }: StatusAlertSpec) {
  return (
    <Alert severity={severity} role={role} sx={{ mt: 2.5, borderRadius: 2 }}>
      {message}
    </Alert>
  );
}

export function ChangeMethodButton({ onClick }: { onClick: () => void }) {
  return (
    <Button fullWidth variant="outlined" size="large" onClick={onClick} sx={secondaryButtonSx}>
      Mudar Forma de Pagamento
    </Button>
  );
}

export function PrimaryCheckoutButton({
  disabled,
  label,
  onClick,
}: {
  disabled: boolean;
  label: string;
  onClick: () => void;
}) {
  return (
    <Button
      fullWidth
      variant="contained"
      size="large"
      disabled={disabled}
      onClick={onClick}
      endIcon={<ArrowForwardRoundedIcon />}
      sx={primaryButtonSx}
    >
      {label}
    </Button>
  );
}

export function MethodCardContent({ method }: { method: PublicPaymentMethod }) {
  const isPix = method.instrumentType === "Pix";
  const Icon = isPix
    ? PixIcon
    : method.instrumentType === "CreditCard"
      ? CreditCardRoundedIcon
      : method.instrumentType === "Boleto"
        ? CheckoutBarcodeIcon
        : ReceiptLongOutlinedIcon;
  return (
    <Stack
      direction="row"
      spacing={{ xs: 1.25, md: 2 }}
      sx={{ alignItems: "center", minWidth: 0, py: 1.25 }}
    >
      <Box
        sx={{
          display: "grid",
          placeItems: "center",
          flexShrink: 0,
          width: { xs: 36, md: 48 },
          height: { xs: 36, md: 48 },
          color: isPix ? checkoutColors.pixAccent : checkoutColors.ink,
        }}
      >
        <Icon sx={{ fontSize: { xs: 28, md: 38 } }} aria-hidden="true" />
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography
          sx={{
            color: checkoutColors.ink,
            fontSize: financeTokens.typography.checkoutSection,
            fontWeight: 700,
            lineHeight: 1.25,
            overflowWrap: "anywhere",
          }}
        >
          {method.name}
        </Typography>
      </Box>
    </Stack>
  );
}

function CheckoutBarcodeIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M2 5h2v14H2zm3 0h1v14H5zm3 0h3v14H8zm4 0h1v14h-1zm3 0h2v14h-2zm3 0h1v14h-1zm3 0h1v14h-1z" />
    </SvgIcon>
  );
}

export function Summary({
  label,
  value,
  divided = false,
}: {
  label: string;
  value: string;
  divided?: boolean;
}) {
  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      sx={{
        justifyContent: "space-between",
        alignItems: { xs: "flex-start", sm: "center" },
        minWidth: 0,
        ...(divided
          ? { borderTop: `1px solid ${checkoutColors.divider}`, pt: { xs: 2, md: 2.5 } }
          : {}),
      }}
    >
      <Typography
        sx={{
          color: checkoutColors.muted,
          fontSize: financeTokens.typography.checkoutLabel,
          lineHeight: 1.4,
          flexShrink: 0,
        }}
      >
        {label}
      </Typography>
      <Typography
        sx={{
          color: checkoutColors.ink,
          fontSize: financeTokens.typography.checkoutLabel,
          fontWeight: 550,
          textAlign: { xs: "left", sm: "right" },
          minWidth: 0,
          overflowWrap: "anywhere",
        }}
      >
        {value}
      </Typography>
    </Stack>
  );
}
