import { Suspense } from "react";
import { PaymentGenerationResultClient } from "@/features/payments/components/PaymentGenerationResultClient";
export default function PaymentResultPage() { return <Suspense fallback={null}><PaymentGenerationResultClient /></Suspense>; }
