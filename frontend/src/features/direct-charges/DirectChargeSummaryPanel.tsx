import { Box, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { financeTokens } from "@/theme/designTokens";
import { formatDate } from "@/utils/date";
import { formatPlate } from "@/utils/plate";
import type { DirectChargeBasics } from "./directChargeDraft";
import { actionGap, fieldGap, panelInset } from "@/theme/layout";

interface DirectChargeSummaryPanelProps {
  customerName: string | undefined;
  basics: DirectChargeBasics;
}

export function DirectChargeSummaryPanel({ customerName, basics }: DirectChargeSummaryPanelProps) {
  function renderSummary() {
    const summaryRows = [
      ["Cliente", customerName ?? "—"],
      ["Contrato", basics.contract],
      ["Placa", formatPlate(basics.plate)],
      ["Vencimento", formatDate(basics.dueDate)],
      ["Centro de custo", basics.costCenter],
      ...(basics.note.trim() ? [["Observação ao cliente", basics.note]] : []),
    ];
    return (
      <PrototypePanel
        title="Dados da cobrança"
        highlighted
        highlightedBackground={financeTokens.colors.stepBackground}
        headerPaddingX={panelInset}
      >
        <Box
          component="dl"
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "minmax(0, 1fr)",
              sm: "repeat(2, minmax(0, 1fr))",
              md: "repeat(5, minmax(0, 1fr))",
            },
            columnGap: fieldGap,
            rowGap: actionGap,
            m: 0,
            px: panelInset,
            py: actionGap,
          }}
        >
          {summaryRows.map(([label, value]) => (
            <Box
              key={label}
              sx={{
                display: "flex",
                flexWrap: "wrap",
                alignItems: "baseline",
                gap: "0.375rem",
                minWidth: 0,
                maxWidth: "100%",
                ...(label === "Observação ao cliente" ? { gridColumn: "1 / -1" } : {}),
              }}
            >
              <Typography component="dt" sx={{ fontWeight: 700, whiteSpace: "nowrap" }}>
                {label}:
              </Typography>
              <Typography component="dd" sx={{ m: 0, minWidth: 0, overflowWrap: "anywhere" }}>
                {value}
              </Typography>
            </Box>
          ))}
        </Box>
      </PrototypePanel>
    );
  }
  return renderSummary();
}
