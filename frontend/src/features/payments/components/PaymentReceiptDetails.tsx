import { Box, Stack, Typography } from "@mui/material";
import { formatCurrency } from "@/utils/currency";

type Props = {
  amount: number;
  receivedAmount: number;
  receivedAt?: string | null;
  settledAt?: string | null;
  settled?: boolean;
  receipts?: { id?: string; endToEndId?: string | null; amount: number; receivedAt: string; paymentMethodName?: string; provider?: string }[] | null;
};

function formatDateTime(value: string) {
  const parsed = new Date(value);
  if (!Number.isFinite(parsed.getTime())) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short", timeZone: "America/Sao_Paulo" }).format(parsed);
}

export function PaymentReceiptDetails({ amount, receivedAmount, receivedAt, settledAt, settled = false, receipts }: Props) {
  if (receivedAmount <= 0) return null;
  const rows: [string, string][] = [["Total recebido", formatCurrency(receivedAmount)]];
  if (!receipts?.length && receivedAt) rows.push(["Recebido em", formatDateTime(receivedAt)]);
  if (settledAt) rows.push(["Baixa confirmada em", formatDateTime(settledAt)]);
  if (!settled) rows.push(["Saldo pendente", formatCurrency(Math.max(0, amount - receivedAmount))]);

  return <Stack spacing={1.25} aria-label="Detalhes do recebimento">
    {rows.map(([label, value]) => <Box key={label} sx={{ display: "grid", gridTemplateColumns: "minmax(130px, 1fr) 2fr", gap: 2 }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>{value}</Typography>
    </Box>)}
    {receipts?.map((receipt, index) => <Box key={receipt.id ?? index} sx={{ borderTop: "1px solid #e5ecf3", pt: 1.25 }}>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>Recebimento {index + 1}</Typography>
      {[["Identificador do recebimento", receipt.id ?? String(index + 1)], ["Valor", formatCurrency(receipt.amount)], ["Data do recebimento", formatDateTime(receipt.receivedAt)],
        ...(receipt.paymentMethodName ? [["Forma de pagamento", receipt.paymentMethodName] as [string, string]] : []),
        ...(receipt.provider ? [["Provedor", receipt.provider] as [string, string]] : []),
        ...(receipt.endToEndId ? [["Referência bancária", receipt.endToEndId] as [string, string]] : [])]
        .map(([label, value]) => <Box key={label} sx={{ display: "grid", gridTemplateColumns: "minmax(130px, 1fr) 2fr", gap: 2, mb: 0.75 }}>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
          <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>{value}</Typography>
        </Box>)}
    </Box>)}
  </Stack>;
}
