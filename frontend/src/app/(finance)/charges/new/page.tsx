import { Suspense } from "react";
import { NewChargeClient } from "@/features/charges/components/NewChargeClient";

export default function NewChargePage() {
  return (
    <Suspense fallback={null}>
      <NewChargeClient />
    </Suspense>
  );
}
