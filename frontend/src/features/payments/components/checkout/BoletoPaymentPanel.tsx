"use client";

import { useState } from "react";
import { Box, Button, Stack, Typography } from "@mui/material";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import ContentCopyRoundedIcon from "@mui/icons-material/ContentCopyRounded";
import DownloadRoundedIcon from "@mui/icons-material/DownloadRounded";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import { copyTextToClipboard } from "@/utils/clipboard";
import { financeTokens } from "@/theme/designTokens";
import type { PublicBoletoDetails } from "../../types/payment.types";

export function BoletoPaymentPanel({ boleto }: { boleto: PublicBoletoDetails }) {
  const [copyStatus, setCopyStatus] = useState<"copied" | "error" | null>(null);
  const bankSlipUrl = safeDocumentUrl(boleto.bankSlipUrl);
  const identificationField = boleto.identificationField?.trim() ?? "";

  async function copyIdentificationField() {
    if (!identificationField) return;
    const result = await copyTextToClipboard(identificationField);
    setCopyStatus(result.ok ? "copied" : "error");
  }

  return (
    <Stack spacing={6} sx={{ flex: 1, justifyContent: "flex-start", mt: 6, minWidth: 0, borderTop: "1px solid #dce4ef", pt: 3 }}>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", minWidth: 0 }}>
        <Box sx={{ display: "grid", placeItems: "center", flexShrink: 0, width: 56, height: 56, borderRadius: "50%", bgcolor: "#e5f0ff", color: "#1457b8" }}>
          <DescriptionOutlinedIcon aria-hidden="true" sx={{ fontSize: 30 }} />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography component="h3" sx={{ color: "#101d3b", fontSize: financeTokens.typography.checkoutSection, fontWeight: 700 }}>
            Boleto disponível
          </Typography>
          <Typography sx={{ mt: 0.5, color: "#5b6d91", fontSize: financeTokens.typography.body, lineHeight: 1.5 }}>
            Pague com a linha digitável ou baixe o boleto em PDF.
          </Typography>
        </Box>
      </Stack>

      {identificationField && (
        <Box sx={{ minWidth: 0, borderTop: "1px solid #dce4ef", pt: 4 }}>
          <Typography component="label" htmlFor="checkout-boleto-identification-field" sx={{ display: "block", mb: 1, color: "#101d3b", fontSize: financeTokens.typography.body, fontWeight: 700, textAlign: "center" }}>
            Linha digitável
          </Typography>
          <Typography component="p" sx={{ m: 0, color: "#101d3b", fontFamily: "monospace", fontSize: "1rem", fontWeight: 700, lineHeight: 1.7, textAlign: "center", overflowWrap: "anywhere" }}>
            {formatIdentificationField(identificationField)}
          </Typography>
          {copyStatus && (
            <Stack role="status" aria-live="polite" direction="row" spacing={0.5} sx={{ mt: 0.75, mb: "1%", alignItems: "center", justifyContent: "center", color: copyStatus === "error" ? "#b42318" : "#247a55" }}>
              {copyStatus === "copied" && <CheckCircleRoundedIcon aria-hidden="true" sx={{ fontSize: 16 }} />}
              <Typography sx={{ fontSize: financeTokens.typography.secondary }}>
                {copyStatus === "error" ? "Não foi possível copiar a linha digitável." : "Linha digitável copiada."}
              </Typography>
            </Stack>
          )}
        </Box>
      )}

      <Stack spacing={3} sx={{ pt: 3 }}>
        {identificationField && (
          <Button fullWidth variant="contained" onClick={() => void copyIdentificationField()}
            startIcon={<ContentCopyRoundedIcon />}
            sx={{ minHeight: 50, borderRadius: "10px", bgcolor: "#073a8c", fontSize: financeTokens.typography.body, fontWeight: 700, textTransform: "none", boxShadow: "0 3px 8px rgba(9, 48, 111, .12)", "&:hover": { bgcolor: "#062f73", boxShadow: "0 4px 10px rgba(9, 48, 111, .18)" } }}>
            {copyStatus === "copied" ? "Linha digitável copiada" : "Copiar linha digitável"}
          </Button>
        )}

        {bankSlipUrl && (
          <Button fullWidth component="a" href={bankSlipUrl} target="_blank" rel="noopener noreferrer" download
            startIcon={<DownloadRoundedIcon />} variant="outlined"
            sx={{ minHeight: 50, borderRadius: "10px", borderColor: "#1877f2", bgcolor: "#fff", color: "#073a8c", fontSize: financeTokens.typography.body, fontWeight: 700, textTransform: "none", "&:hover": { borderColor: "#073a8c", bgcolor: "#edf4ff" } }}>
            Baixar boleto (PDF)
          </Button>
        )}
      </Stack>
    </Stack>
  );
}

function safeDocumentUrl(value?: string | null) {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" && !url.username && !url.password ? url.href : null;
  } catch {
    return null;
  }
}

function formatIdentificationField(value: string) {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 47 || digits !== value) return value;
  return `${digits.slice(0, 5)}.${digits.slice(5, 10)} ${digits.slice(10, 15)}.${digits.slice(15, 21)} ${digits.slice(21, 26)}.${digits.slice(26, 32)} ${digits.slice(32, 33)} ${digits.slice(33)}`;
}
