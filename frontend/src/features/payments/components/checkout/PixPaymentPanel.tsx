"use client";

import { useState } from "react";
import { Box, Button, OutlinedInput, Stack, Typography } from "@mui/material";
import { QRCodeSVG } from "qrcode.react";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import ContentCopyRoundedIcon from "@mui/icons-material/ContentCopyRounded";
import AccessTimeRoundedIcon from "@mui/icons-material/AccessTimeRounded";
import { copyTextToClipboard } from "@/utils/clipboard";
import { financeTokens } from "@/theme/designTokens";
import type { PublicPaymentAttempt } from "../../types/payment.types";

export function PixPaymentPanel({ attempt, methodName }: { attempt: PublicPaymentAttempt; methodName?: string }) {
  const [copyStatus, setCopyStatus] = useState<"idle" | "copied" | "error">("idle");
  const formatExpiration = (value: string) => new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short", timeZone: "America/Sao_Paulo" }).format(new Date(value));
  async function handleCopyPix() {
    if (!attempt.brCode) return;
    const result = await copyTextToClipboard(attempt.brCode);
    setCopyStatus(result.ok ? "copied" : "error");
  }
  return (
                <Stack spacing={2.5} sx={{ alignItems: "center", mt: 3, p: { xs: 2, sm: 3 }, minWidth: 0, borderRadius: 2, bgcolor: "#f5faff", border: "1px solid #dce8f8" }}>
                  <Box sx={{ textAlign: "center" }}>
                    <Typography component="h3" sx={{ color: "#101d3b", fontSize: financeTokens.typography.checkoutSection, fontWeight: 700 }}>
                      {methodName ? `${methodName} gerado` : "Pagamento gerado"}
                    </Typography>
                    <Typography sx={{ mt: 0.75, color: "#5b6d91", fontSize: financeTokens.typography.body, lineHeight: 1.5 }}>
                      Escaneie o QR Code no aplicativo do seu banco.
                    </Typography>
                  </Box>
                  <Box sx={{ width: 256, maxWidth: "100%", boxSizing: "border-box", bgcolor: "#fff", p: 1.5, border: "1px solid #dce4ef", borderRadius: "16px", boxShadow: "0 4px 16px rgba(31, 56, 92, .06)", lineHeight: 0 }}>
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
                  {attempt.expiresAt && (
                    <Stack direction="row" spacing={0.75} sx={{ alignItems: "center", justifyContent: "center", color: "#5b6d91" }}>
                      <AccessTimeRoundedIcon aria-hidden="true" sx={{ fontSize: 16, flexShrink: 0 }} />
                      <Typography sx={{ fontSize: financeTokens.typography.secondary, lineHeight: 1.5 }}>Pix válido até {formatExpiration(attempt.expiresAt)}</Typography>
                    </Stack>
                  )}
                  <Box sx={{ width: "100%", minWidth: 0, pt: 2.5, borderTop: "1px solid #dce8f8" }}>
                    <Typography component="label" htmlFor="checkout-pix-code" sx={{ display: "block", mb: 1, color: "#101d3b", fontSize: financeTokens.typography.body, fontWeight: 600 }}>
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
                          bgcolor: "#fff",
                          borderRadius: "10px",
                          color: "#5b6d91",
                          "& .MuiOutlinedInput-input": { p: 1, fontFamily: "monospace", fontSize: financeTokens.typography.secondary, textOverflow: "ellipsis" },
                          "& .MuiOutlinedInput-notchedOutline": { borderColor: "#cbd7e7" },
                          "&.Mui-focused .MuiOutlinedInput-notchedOutline": { borderColor: "#2385ff" },
                        }}
                      />
                      <Button
                        variant="outlined"
                        onClick={handleCopyPix}
                        startIcon={copyStatus === "copied" ? <CheckCircleRoundedIcon /> : <ContentCopyRoundedIcon />}
                        sx={{
                          flexShrink: 0,
                          minHeight: 44,
                          px: 2,
                          borderRadius: "10px",
                          borderColor: "#cbd7e7",
                          bgcolor: "#fff",
                          color: "#073a8c",
                          textTransform: "none",
                          fontWeight: 700,
                          "&:hover": { borderColor: "#073a8c", bgcolor: "#edf4ff" },
                          "&:focus-visible": { outline: "3px solid #2385ff", outlineOffset: 2 },
                        }}
                      >
                        {copyStatus === "copied" ? "Código copiado" : "Copiar código Pix"}
                      </Button>
                    </Stack>
                    <Typography role="status" aria-live="polite" sx={{ mt: 1, color: copyStatus === "error" ? "#b42318" : copyStatus === "copied" ? "#247a55" : "#5b6d91", fontSize: financeTokens.typography.secondary, lineHeight: 1.5 }}>
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
