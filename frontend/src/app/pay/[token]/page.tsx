import { backendResponse } from "@/services/api/api-client.server";
import { PublicPaymentCheckoutClient } from "@/features/payments/components/checkout/PublicPaymentCheckoutClient";
import type { PublicPaymentCheckout } from "@/features/payments/types/payment.types";

export const dynamic = "force-dynamic";
export const revalidate = 0;
export const metadata = { referrer: "no-referrer" };

export default async function PublicPaymentPage({ params }: PageProps<"/pay/[token]">) {
  const { token } = await params;
  if (!/^[a-f0-9]{64}$/i.test(token)) {
    return <PublicPaymentCheckoutClient token={token} checkout={null} />;
  }

  let checkout: PublicPaymentCheckout | null = null;
  let temporaryError = false;
  try {
    const response = await backendResponse(`/api/payments/${encodeURIComponent(token)}`);
    if (response.ok) checkout = (await response.json()) as PublicPaymentCheckout;
    else temporaryError = response.status >= 500 || response.status === 408 || response.status === 429;
  } catch { temporaryError = true; }
  return (
    <PublicPaymentCheckoutClient
      token={token}
      checkout={checkout}
      temporaryError={temporaryError}
    />
  );
}
