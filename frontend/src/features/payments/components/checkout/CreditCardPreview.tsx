"use client";

import { checkoutColors } from "@/theme/checkoutTheme";
import { Box, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

export type CardPreviewField = "number" | "name" | "expiry" | "cvv" | null;

type Props = {
  number: string;
  name: string;
  expiry: string;
  cvvLength: number;
  focusedField: CardPreviewField;
};

function formatCardNumber(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 19);
  return digits.replace(/(\d{4})(?=\d)/g, "$1 ") || "•••• •••• •••• ••••";
}

function formatCardExpiry(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 6);
  return `${digits.slice(0, 2).padEnd(2, "M")}/${digits.slice(2).padEnd(4, "Y")}`;
}

function formatCvvLength(length: number) {
  const safeLength = Number.isFinite(length) ? Math.min(4, Math.max(0, Math.floor(length))) : 0;
  return "•".repeat(safeLength);
}

export function CreditCardPreview({ number, name, expiry, cvvLength, focusedField }: Props) {
  const isShowingBack = focusedField === "cvv";
  const cardNumber = formatCardNumber(number);
  const cardHolderName = name.trim().toLocaleUpperCase("pt-BR") || "NOME NO CARTÃO";
  const securityCode = formatCvvLength(cvvLength);
  const activeOutline = `0 0 0 2px ${checkoutColors.action}`;

  return (
    <Box
      aria-hidden="true"
      sx={{ containerType: "inline-size", maxWidth: 340, mx: "auto", width: "100%" }}
    >
      <Box sx={{ perspective: "1000px", width: "100%" }}>
        <Box
          sx={{
            aspectRatio: "1.586",
            position: "relative",
            transform: isShowingBack ? "rotateY(180deg)" : "none",
            transformStyle: "preserve-3d",
            transition: "transform 420ms cubic-bezier(.2,.7,.2,1)",
            "@media (prefers-reduced-motion: reduce)": { transition: "none" },
          }}
        >
          <Box
            sx={{
              backfaceVisibility: "hidden",
              bgcolor: financeTokens.colors.header,
              borderRadius: "10px",
              color: checkoutColors.white,
              inset: 0,
              overflow: "hidden",
              position: "absolute",
              px: "7%",
              py: "8%",
            }}
          >
            <Box
              component="svg"
              viewBox="0 0 48 38"
              sx={{
                color: checkoutColors.cardTextMuted,
                height: "auto",
                left: "7%",
                position: "absolute",
                top: "30%",
                width: "12%",
              }}
            >
              <rect
                x="1"
                y="1"
                width="46"
                height="36"
                rx="6"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
              />
              <path
                d="M16 1v36M32 1v36M1 13h15m16 0h15M1 25h15m16 0h15"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
              />
            </Box>

            <Typography
              component="span"
              sx={{
                borderRadius: "2px",
                fontFamily: "monospace",
                fontSize: "clamp(0.72rem, 4.25cqw, 0.92rem)",
                fontVariantNumeric: "tabular-nums",
                left: "7%",
                letterSpacing: ".035em",
                outline: focusedField === "number" ? activeOutline : "none",
                position: "absolute",
                right: "7%",
                top: "56%",
                whiteSpace: "nowrap",
              }}
            >
              {cardNumber}
            </Typography>

            <Box
              sx={{
                alignItems: "flex-end",
                bottom: "10%",
                display: "flex",
                gap: 1.5,
                left: "7%",
                position: "absolute",
                right: "7%",
              }}
            >
              <Typography
                component="span"
                noWrap
                sx={{
                  borderRadius: "2px",
                  flex: 1,
                  fontSize: "clamp(0.65rem, 3.8cqw, 0.84rem)",
                  minWidth: 0,
                  outline: focusedField === "name" ? activeOutline : "none",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {cardHolderName}
              </Typography>
              <Box
                sx={{
                  borderRadius: "2px",
                  display: "grid",
                  flexShrink: 0,
                  gap: 0.25,
                  outline: focusedField === "expiry" ? activeOutline : "none",
                }}
              >
                <Typography
                  component="span"
                  sx={{
                    color: checkoutColors.cardTextMuted,
                    fontSize: "clamp(0.48rem, 2.5cqw, 0.62rem)",
                    letterSpacing: ".06em",
                  }}
                >
                  VALIDADE
                </Typography>
                <Typography component="span" sx={{ fontSize: "clamp(0.65rem, 3.8cqw, 0.84rem)" }}>
                  {formatCardExpiry(expiry)}
                </Typography>
              </Box>
            </Box>
          </Box>

          <Box
            sx={{
              backfaceVisibility: "hidden",
              bgcolor: financeTokens.colors.header,
              borderRadius: "10px",
              color: checkoutColors.white,
              inset: 0,
              overflow: "hidden",
              position: "absolute",
              transform: "rotateY(180deg)",
            }}
          >
            <Box
              sx={{
                bgcolor: checkoutColors.cardStripeShade,
                height: "20%",
                mt: "12%",
                width: "100%",
              }}
            />
            <Box
              sx={{
                alignItems: "stretch",
                display: "flex",
                gap: 1.5,
                height: "20%",
                mx: "7%",
                mt: "7%",
              }}
            >
              <Box sx={{ bgcolor: checkoutColors.cardStripe, borderRadius: "2px", flex: 1 }} />
              <Box
                sx={{
                  alignContent: "center",
                  borderRadius: "2px",
                  display: "grid",
                  gap: 0.25,
                  minWidth: 42,
                  outline: activeOutline,
                  px: 0.5,
                }}
              >
                <Typography
                  component="span"
                  sx={{
                    color: checkoutColors.cardTextMuted,
                    fontSize: "clamp(0.48rem, 2.5cqw, 0.62rem)",
                    letterSpacing: ".06em",
                  }}
                >
                  CVV
                </Typography>
                <Typography component="span" sx={{ fontSize: "clamp(0.65rem, 3.8cqw, 0.84rem)" }}>
                  {securityCode || "—"}
                </Typography>
              </Box>
            </Box>
            <Typography
              component="span"
              sx={{
                color: checkoutColors.cardTextMuted,
                display: "block",
                fontSize: "clamp(0.58rem, 3.2cqw, 0.72rem)",
                mx: "7%",
                mt: "6%",
              }}
            >
              Código de segurança
            </Typography>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
