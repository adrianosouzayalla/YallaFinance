import Link from "next/link";
import { Box, Button, Stack, Typography } from "@mui/material";
import { formatCurrency } from "@/utils/currency";
import { buildPaymentEntryUrl } from "../paymentEntry";
import { financeTokens } from "@/theme/designTokens";
import type { PaymentCharge } from "../types/payment.types";

export function PaymentSelectionBar({ rows, onClear }: { rows: PaymentCharge[]; onClear(): void }) {
  if (!rows.length) return null;
  return <Box aria-live="polite" sx={{ borderTop: "1px solid", borderColor: "divider", px: 2.5, py: 1.5 }}>
    <Stack direction="row" spacing={2} sx={{ alignItems: "center", justifyContent: "space-between", flexWrap: "wrap" }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: "baseline" }}>
        <Typography sx={{ fontSize: financeTokens.typography.body, fontWeight: 700 }}>{rows.length} {rows.length === 1 ? "cobrança selecionada" : "cobranças selecionadas"}</Typography>
        <Typography sx={{ fontSize: financeTokens.typography.secondary }}>Total: {formatCurrency(rows.reduce((total, row) => total + row.amount, 0))}</Typography>
      </Stack>
      <Stack direction="row" spacing={1}>
        <Button onClick={onClear}>Limpar seleção</Button>
        {rows.length <= 100 ? <Button component={Link} href={buildPaymentEntryUrl(rows.map((row) => row.id))} variant="contained">Gerar pagamento</Button>
          : <Button disabled variant="contained">Selecione até 100 cobranças</Button>}
      </Stack>
    </Stack>
  </Box>;
}
