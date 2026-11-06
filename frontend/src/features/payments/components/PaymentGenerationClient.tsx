"use client";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { Alert, Button, Stack } from "@mui/material";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { LoadingState } from "@/components/ui/LoadingState";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { parsePaymentChargeIds, buildPaymentEntryUrl } from "../paymentEntry";
import { usePaymentPreparation } from "../hooks/usePaymentPreparation";
import { PaymentRequestForm } from "./PaymentRequestForm";

export function PaymentGenerationClient() {
  const search = useSearchParams();
  const router = useRouter();
  const { contextId } = useSessionContext();
  let ids: string[] = [];
  try {
    ids = parsePaymentChargeIds(search.get("chargeIds"));
  } catch {}
  const query = usePaymentPreparation(ids);
  if (query.isFetching && !query.data)
    return <LoadingState mode="blocking" message="Validando cobranças..." />;
  if (!ids.length)
    return (
      <Alert
        severity="warning"
        action={
          <Button component={Link} href="/payments">
            Selecionar cobranças
          </Button>
        }
      >
        Selecione de 1 a 100 cobranças válidas.
      </Alert>
    );
  if (query.isError)
    return (
      <Alert
        severity="error"
        action={<Button onClick={() => void query.refetch()}>Tentar novamente</Button>}
      >
        Não foi possível validar as cobranças selecionadas.
      </Alert>
    );
  if (!query.data) return <LoadingState mode="blocking" message="Validando cobranças..." />;
  return (
    <Stack spacing={3}>
      <FinancePageTitle navigationHref="/payments">Gerar pagamento</FinancePageTitle>
      <PaymentRequestForm
        key={`${contextId}:${[...ids].sort().join(",")}`}
        preparation={query.data}
        onRemove={(id) => {
          const remaining = ids.filter((value) => value !== id);
          router.replace(remaining.length ? buildPaymentEntryUrl(remaining) : "/payments");
        }}
      />
    </Stack>
  );
}
