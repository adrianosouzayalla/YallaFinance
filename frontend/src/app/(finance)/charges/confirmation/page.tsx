import { ChargeConfirmationClient } from "@/features/charges/components/ChargeConfirmationClient";
import { ChargeDataBoundary } from "@/features/charges/components/ChargeDataBoundary";

export default function ChargeConfirmationPage() {
  return (
    <ChargeDataBoundary>
      <ChargeConfirmationClient />
    </ChargeDataBoundary>
  );
}
