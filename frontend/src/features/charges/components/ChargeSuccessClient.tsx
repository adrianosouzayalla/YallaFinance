"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";

import { useChargeDraft } from "@/features/charges/context/ChargeDraftProvider";
import { useCharge } from "@/features/charges/hooks/useCharge";
import { useCategories } from "@/features/catalogs/hooks/useCategories";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { LoadingState } from "@/components/ui/LoadingState";
import { ChargeResult } from "./ChargeResult";
import { buildPaymentEntryUrl } from "@/features/payments/paymentEntry";
import { usePaymentPreparation } from "@/features/payments/hooks/usePaymentPreparation";

export function ChargeSuccessClient() {
  const searchParams = useSearchParams();
  const { clearChargeIntent } = useChargeDraft();
  const chargeId = searchParams.get("chargeId");
  const chargeQuery = useCharge(chargeId);
  const charge = chargeQuery.data ?? undefined;
  let paymentEntryUrl: string | null = null;
  try {
    if (charge) paymentEntryUrl = buildPaymentEntryUrl([charge.id]);
  } catch {
    /* Invalid IDs cannot start payment generation. */
  }
  const paymentQuery = usePaymentPreparation(paymentEntryUrl && charge ? [charge.id] : []);
  const paymentCharge = paymentQuery.data?.charges.find((row) => row.id === charge?.id) ?? null;
  const categoryQuery = useCategories();
  const categories = categoryQuery.data ?? [];
  const categoryName = categories.find((item) => item.id === charge?.categoryId)?.name ?? "";

  const [manualRefresh, setManualRefresh] = useState(false);
  async function refreshCharge() {
    setManualRefresh(true);
    try {
      await chargeQuery.refetch();
    } finally {
      setManualRefresh(false);
    }
  }

  useEffect(() => {
    if (charge !== undefined) {
      clearChargeIntent();
    }
  }, [charge, clearChargeIntent]);

  if (chargeQuery.isFetching && !chargeQuery.data) {
    return <LoadingState mode="blocking" message="Buscando cobrança..." />;
  }

  if (chargeQuery.isError) {
    return (
      <Alert
        severity="error"
        action={<Button onClick={() => void chargeQuery.refetch()}>Tentar novamente</Button>}
      >
        Não foi possível carregar a cobrança.
      </Alert>
    );
  }

  if (charge === undefined) {
    return !chargeId || !chargeQuery.isPending ? (
      <Alert severity="error">Não foi possível recuperar os dados da cobrança gerada.</Alert>
    ) : (
      <LoadingState mode="blocking" message="Buscando cobrança..." />
    );
  }

  if (
    !charge.processing &&
    !charge.items &&
    charge.financialIntegration &&
    charge.financialIntegration.outcome !== "Completed"
  ) {
    return (
      <Alert severity="error">
        Cobrança criada no Yalla ({charge.reference}), com integração{" "}
        {charge.financialIntegration.outcome === "Rejected" ? "rejeitada" : "indeterminada"}.
        Confira a integração antes de tentar novamente.
      </Alert>
    );
  }

  return (
    <>
      <LoadingState
        mode="blocking"
        open={
          manualRefresh ||
          categoryQuery.isPending ||
          Boolean(paymentEntryUrl && paymentQuery.isFetching)
        }
        message={
          manualRefresh
            ? "Atualizando cobrança..."
            : paymentQuery.isFetching
              ? "Consultando pagamento..."
              : "Carregando dados da cobrança..."
        }
      />
      <Stack spacing={3}>
        <Box>
          <FinancePageTitle navigationHref="/charges/new">
            {charge.items ? "Cobrança registrada" : "Cobrança gerada com sucesso"}
          </FinancePageTitle>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            {charge.items
              ? "Consulte abaixo os lançamentos financeiros de cada item."
              : "A cobrança PIX foi criada com sucesso."}
          </Typography>
        </Box>
        <ChargeStepper activeStep={2} />
        <ChargeResult
          key={charge.id}
          charge={charge}
          categories={categories}
          categoryName={categoryName}
          refreshingIntegration={chargeQuery.isFetching}
          onRefreshIntegration={() => void refreshCharge()}
          payment={{
            charge: paymentCharge,
            href: paymentEntryUrl,
            isFetching: paymentQuery.isFetching,
            isError: paymentQuery.isError,
            onRetry: () => void paymentQuery.refetch(),
          }}
          actions={
            <Button
              component={Link}
              href="/debts"
              variant="outlined"
              startIcon={<ArrowBackOutlinedIcon />}
            >
              Voltar para consulta de débitos
            </Button>
          }
        />
      </Stack>
    </>
  );
}
