import { Box, Button, Divider, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { formatCurrency } from "@/utils/currency";
import type { PaymentCharge } from "@/services/api/payments/payment.types";
import { PaymentStatusBadge } from "./PaymentStatusBadge";

export function PaymentCart({
  charges,
  disabled,
  onRemove,
}: {
  charges: PaymentCharge[];
  disabled: boolean;
  onRemove(id: string): void;
}) {
  return (
    <PrototypePanel title="Cobranças selecionadas">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        {charges.map((charge) => {
          return (
            <Box key={charge.id}>
              <Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>
                Cobrança {charge.reference}
              </Typography>
              <Typography color="text.secondary">
                {charge.customerName} · Contrato {charge.contract}
              </Typography>
              <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center" }}>
                <Typography sx={{ fontWeight: 700 }}>{formatCurrency(charge.amount)}</Typography>
                <Button size="small" disabled={disabled} onClick={() => onRemove(charge.id)}>
                  Remover
                </Button>
              </Stack>
              <PaymentStatusBadge charge={charge} />
              <Divider sx={{ mt: 1.5 }} />
            </Box>
          );
        })}
        <Stack direction="row" sx={{ justifyContent: "space-between" }}>
          <Typography>
            {charges.length} {charges.length === 1 ? "cobrança" : "cobranças"}
          </Typography>
          <Typography sx={{ fontWeight: 700 }}>
            Total: {formatCurrency(charges.reduce((sum, row) => sum + row.amount, 0))}
          </Typography>
        </Stack>
      </Stack>
    </PrototypePanel>
  );
}
