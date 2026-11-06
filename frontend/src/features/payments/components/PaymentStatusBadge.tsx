import { StatusBadge, type StatusBadgeTone } from "@/components/ui/StatusBadge";
import {
  paymentChargeStatus,
  paymentRequestChargeStatus,
  type PaymentChargeStatusInput,
} from "../paymentStatus";
import type { PaymentRequest } from "@/services/api/payments/payment.types";

const statusTones: Record<string, StatusBadgeTone> = {
  Disponível: "info",
  "Aguardando pagamento": "warning",
  "Link vencido": "orange",
  Pago: "success",
};

export function PaymentStatusBadge({
  charge,
  request,
}: {
  charge: PaymentChargeStatusInput;
  request?: PaymentRequest;
}) {
  const status = request
    ? paymentRequestChargeStatus(request, charge)
    : paymentChargeStatus(charge);
  return <StatusBadge label={status.label} tone={statusTones[status.label] ?? status.tone} />;
}
