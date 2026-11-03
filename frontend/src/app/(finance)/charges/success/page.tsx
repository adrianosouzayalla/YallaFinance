import { Suspense } from "react";
import { ChargeSuccessClient } from "@/features/charges/components/ChargeSuccessClient";

export default function ChargeSuccessPage() {
  return (
    <Suspense fallback={null}>
      <ChargeSuccessClient />
    </Suspense>
  );
}
