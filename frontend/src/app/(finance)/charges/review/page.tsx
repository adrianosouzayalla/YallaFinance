import { Suspense } from "react";
import { ChargeReviewClient } from "@/features/charges/components/ChargeReviewClient";
import { ChargeDataBoundary } from "@/features/charges/components/ChargeDataBoundary";

export default function ChargeReviewPage() {
  return (
    <Suspense fallback={null}>
      <ChargeDataBoundary>
        <ChargeReviewClient />
      </ChargeDataBoundary>
    </Suspense>
  );
}
