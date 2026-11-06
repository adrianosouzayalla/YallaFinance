import { Box, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { formatDate } from "@/utils/date";
import { paymentDateOnly } from "../paymentDate";
import { paymentChargeStatus } from "../paymentStatus";
import type { PaymentCharge } from "@/services/api/payments/payment.types";
import { PaymentStatusBadge } from "./PaymentStatusBadge";
import { PaymentReceiptDetails } from "./PaymentReceiptDetails";

export function PaymentChargeSummary({ charge }: { charge: PaymentCharge }) {
  const request = charge.activity?.latestRequest ?? charge.activePaymentRequest;
  const status = paymentChargeStatus(charge);
  const methods = charge.activity?.latestRequest?.authorizedPaymentMethods;
  return (
    <PrototypePanel title="Pagamento" action={<PaymentStatusBadge charge={charge} />}>
      <Stack spacing={1.5} sx={{ p: 2.5 }}>
        <Typography variant="body2" color="text.secondary">
          {status.detail}
        </Typography>
        {charge.receivedAmount !== undefined && charge.receivedAmount > 0 && (
          <PaymentReceiptDetails
            amount={charge.amount}
            receivedAmount={charge.receivedAmount}
            receivedAt={charge.receivedAt}
            settledAt={charge.settledAt}
            settled={charge.settled}
            receipts={charge.receipts}
          />
        )}
        {!charge.settled && request && (
          <Box
            sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "2fr 1fr 1fr" }, gap: 2 }}
          >
            {[
              ["Solicitação", request.id],
              ["Gerada em", formatDate(paymentDateOnly(request.createdAt))],
              ["Validade", formatDate(paymentDateOnly(request.expiresAt))],
            ].map(([label, value]) => (
              <Box key={label} sx={{ minWidth: 0 }}>
                <Typography variant="caption" color="text.secondary">
                  {label}
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>
                  {value}
                </Typography>
              </Box>
            ))}
          </Box>
        )}
        {!charge.settled && !!methods?.length && (
          <Typography variant="body2">Formas de pagamento: {methods.join(" · ")}</Typography>
        )}
      </Stack>
    </PrototypePanel>
  );
}
