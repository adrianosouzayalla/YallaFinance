"use client";

import { useEffect, useState } from "react";
import { Box, Button, OutlinedInput, Stack, Typography } from "@mui/material";
import { QRCodeSVG } from "qrcode.react";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import ContentCopyRoundedIcon from "@mui/icons-material/ContentCopyRounded";
import AccessTimeRoundedIcon from "@mui/icons-material/AccessTimeRounded";
import { copyTextToClipboard } from "@/utils/clipboard";
import { financeTokens } from "@/theme/designTokens";
import type { PublicPaymentAttempt } from "@/services/api/payments/payment.types";
import { checkoutColors } from "@/theme/checkoutTheme";

const URGENT_REMAINING_MS = 5 * 60_000;

/** Remaining time as mm:ss, or h:mm:ss from one hour on. Rounds up so the last second still shows. */
export function formatPixCountdown(remainingMs: number): string {
  const totalSeconds = Number.isFinite(remainingMs)
    ? Math.max(0, Math.ceil(remainingMs / 1_000))
    : 0;
  const hours = Math.floor(totalSeconds / 3_600);
  const minutes = Math.floor((totalSeconds % 3_600) / 60);
  const seconds = totalSeconds % 60;
  const pad = (value: number) => String(value).padStart(2, "0");
  return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${pad(minutes)}:${pad(seconds)}`;
}

function PixCountdown({ expiresAt }: { expiresAt: string }) {
  const expiryTime = new Date(expiresAt).getTime();
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1_000);
    return () => window.clearInterval(timer);
  }, []);
  const remainingMs = Math.max(0, expiryTime - now);
  const urgent = remainingMs <= URGENT_REMAINING_MS;
  return (
    <Typography
      role="timer"
      sx={{
        textAlign: "center",
        color: urgent ? checkoutColors.danger : checkoutColors.muted,
        fontSize: financeTokens.typography.body,
        fontWeight: urgent ? 700 : 600,
      }}
    >
      Este código vence em {formatPixCountdown(remainingMs)}
    </Typography>
  );
}

export function PixPaymentPanel({
  attempt,
  methodName,
}: {
  attempt: PublicPaymentAttempt;
  methodName?: string;
}) {
  const [copyStatus, setCopyStatus] = useState<"idle" | "copied" | "error">("idle");
  const formatExpiration = (value: string) =>
    new Intl.DateTimeFormat("pt-BR", {
      dateStyle: "short",
      timeStyle: "short",
      timeZone: "America/Sao_Paulo",
    }).format(new Date(value));
  async function handleCopyPix() {
    if (!attempt.brCode) return;
    const result = await copyTextToClipboard(attempt.brCode);
    setCopyStatus(result.ok ? "copied" : "error");
  }
  return (
    <Stack
      spacing={2.5}
      sx={{
        alignItems: "center",
        mt: 3,
        p: { xs: 2, sm: 3 },
        minWidth: 0,
        borderRadius: 2,
        bgcolor: checkoutColors.pixPanelBackground,
        border: `1px solid ${checkoutColors.pixPanelBorder}`,
      }}
    >
      <Box sx={{ textAlign: "center" }}>
        <Typography
          component="h3"
          sx={{
            color: checkoutColors.ink,
            fontSize: financeTokens.typography.checkoutSection,
            fontWeight: 700,
          }}
        >
          {methodName ? `${methodName} gerado` : "Pagamento gerado"}
        </Typography>
        <Typography
          sx={{
            mt: 0.75,
            color: checkoutColors.muted,
            fontSize: financeTokens.typography.body,
            lineHeight: 1.5,
          }}
        >
          Escaneie o QR Code no aplicativo do seu banco.
        </Typography>
      </Box>
      <Box
        sx={{
          width: 256,
          maxWidth: "100%",
          boxSizing: "border-box",
          bgcolor: checkoutColors.white,
          p: 1.5,
          border: `1px solid ${checkoutColors.divider}`,
          borderRadius: "16px",
          boxShadow: checkoutColors.qrShadow,
          lineHeight: 0,
        }}
      >
        <QRCodeSVG
          value={attempt.brCode!}
          size={232}
          level="M"
          marginSize={4}
          title="QR Code para pagamento Pix"
          role="img"
          aria-label="QR Code para pagamento Pix"
          style={{ display: "block", width: "100%", height: "auto" }}
        />
      </Box>
      {attempt.expiresAt && <PixCountdown expiresAt={attempt.expiresAt} />}
      {attempt.expiresAt && (
        <Stack
          direction="row"
          spacing={0.75}
          sx={{ alignItems: "center", justifyContent: "center", color: checkoutColors.muted }}
        >
          <AccessTimeRoundedIcon aria-hidden="true" sx={{ fontSize: 16, flexShrink: 0 }} />
          <Typography sx={{ fontSize: financeTokens.typography.secondary, lineHeight: 1.5 }}>
            Pix válido até {formatExpiration(attempt.expiresAt)}
          </Typography>
        </Stack>
      )}
      <Box
        sx={{
          width: "100%",
          minWidth: 0,
          pt: 2.5,
          borderTop: `1px solid ${checkoutColors.pixPanelBorder}`,
        }}
      >
        <Typography
          component="label"
          htmlFor="checkout-pix-code"
          sx={{
            display: "block",
            mb: 1,
            color: checkoutColors.ink,
            fontSize: financeTokens.typography.body,
            fontWeight: 600,
          }}
        >
          Pix copia e cola
        </Typography>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ minWidth: 0 }}>
          <OutlinedInput
            id="checkout-pix-code"
            fullWidth
            readOnly
            value={attempt.brCode!}
            onFocus={(event) => event.target.select()}
            slotProps={{ input: { "aria-label": "Pix copia e cola", spellCheck: false } }}
            sx={{
              minWidth: 0,
              bgcolor: checkoutColors.white,
              borderRadius: "10px",
              color: checkoutColors.muted,
              "& .MuiOutlinedInput-input": {
                p: 1,
                fontFamily: "monospace",
                fontSize: financeTokens.typography.secondary,
                textOverflow: "ellipsis",
              },
              "& .MuiOutlinedInput-notchedOutline": { borderColor: checkoutColors.secondaryBorder },
              "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
                borderColor: checkoutColors.focus,
              },
            }}
          />
          <Button
            variant="outlined"
            onClick={handleCopyPix}
            startIcon={
              copyStatus === "copied" ? <CheckCircleRoundedIcon /> : <ContentCopyRoundedIcon />
            }
            sx={{
              flexShrink: 0,
              minHeight: 44,
              px: 2,
              borderRadius: "10px",
              borderColor: checkoutColors.secondaryBorder,
              bgcolor: checkoutColors.white,
              color: checkoutColors.action,
              textTransform: "none",
              fontWeight: 700,
              "&:hover": {
                borderColor: checkoutColors.action,
                bgcolor: checkoutColors.secondaryHover,
              },
              "&:focus-visible": { outline: `3px solid ${checkoutColors.focus}`, outlineOffset: 2 },
            }}
          >
            {copyStatus === "copied" ? "Código copiado" : "Copiar código Pix"}
          </Button>
        </Stack>
        <Typography
          role="status"
          aria-live="polite"
          sx={{
            mt: 1,
            color:
              copyStatus === "error"
                ? checkoutColors.danger
                : copyStatus === "copied"
                  ? checkoutColors.success
                  : checkoutColors.muted,
            fontSize: financeTokens.typography.secondary,
            lineHeight: 1.5,
          }}
        >
          {copyStatus === "error"
            ? "Não foi possível copiar. Selecione o código e copie manualmente."
            : copyStatus === "copied"
              ? "Pix copia e cola copiado. Cole na área Pix do seu banco."
              : "Copie o código e cole na área Pix do seu banco."}
        </Typography>
      </Box>
    </Stack>
  );
}
