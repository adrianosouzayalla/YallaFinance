import { DirectChargeSuccessClient } from "@/features/direct-charges/DirectChargeClient";

export default async function DirectChargeSuccessPage({
  searchParams,
}: {
  searchParams: Promise<{ chargeId?: string | string[] }>;
}) {
  const { chargeId } = await searchParams;
  return <DirectChargeSuccessClient chargeId={typeof chargeId === "string" ? chargeId : null} />;
}
