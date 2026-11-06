import { Suspense } from "react";
import { PaymentGenerationClient } from "@/features/payments/components/PaymentGenerationClient";
export default function NewPaymentPage() {
  return (
    <Suspense fallback={null}>
      <PaymentGenerationClient />
    </Suspense>
  );
}
