"use client";

import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { LoadingState } from "@/components/ui/LoadingState";
import { OperationalIndicatorCard } from "@/features/dashboard/components/OperationalIndicatorCard";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboardSummary";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { useState } from "react";
import { financeTokens } from "@/theme/designTokens";

export function HomeIndicators() {
  const summary = useDashboardSummary();
  const dashboard = summary.data;
  const [manualRefresh, setManualRefresh] = useState(false);

  const retry = async () => {
    setManualRefresh(true);
    try {
      await summary.refetch({ cancelRefetch: false });
    } finally {
      setManualRefresh(false);
    }
  };

  return (
    <>
      <LoadingState
        mode="blocking"
        open={summary.isPending || manualRefresh}
        message={manualRefresh ? "Atualizando indicadores..." : "Carregando indicadores..."}
      />
      <Stack spacing={3}>
        {summary.isError ? (
          <Alert
            severity="error"
            action={
              <Button disabled={summary.isFetching} onClick={() => void retry()}>
                Tentar novamente
              </Button>
            }
          >
            {dashboard
              ? "Não foi possível atualizar os indicadores. Os valores anteriores foram mantidos."
              : "Não foi possível carregar os indicadores."}
          </Alert>
        ) : null}
        <Box>
          <FinancePageTitle navigationHref="/home">Visão geral</FinancePageTitle>
          <Typography
            color="text.secondary"
            sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}
          >
            Acompanhe os débitos e inicie uma cobrança.
          </Typography>
        </Box>

        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, minmax(0, 1fr))",
              lg: "repeat(4, minmax(0, 1fr))",
            },
          }}
        >
          <OperationalIndicatorCard
            actionLabel="Ver débitos"
            accent={financeTokens.colors.accentBlue}
            description="Débitos em aberto que ainda não têm cobrança iniciada."
            href="/debts?status=Open"
            icon="pending"
            label="Aguardando cobrança"
            secondaryLabel="valor total"
            secondaryValue={dashboard?.awaitingCharge.amount ?? 0}
            value={dashboard?.awaitingCharge.count ?? 0}
            valueLabel="débitos"
          />
          <OperationalIndicatorCard
            actionLabel="Ver débitos"
            accent={financeTokens.colors.accentGreen}
            description="Débitos em cobrança no momento."
            href="/debts?status=InCollection"
            icon="active"
            label="Cobranças em andamento"
            secondaryLabel="valor em cobrança"
            secondaryValue={dashboard?.activeCharges.amount ?? 0}
            value={dashboard?.activeCharges.count ?? 0}
            valueLabel="débitos"
          />
          <OperationalIndicatorCard
            actionLabel="Ver vencidos"
            accent={financeTokens.colors.accentOrange}
            description="Valor pendente após a data de vencimento."
            href="/debts?status=Open&sort=overdue"
            icon="overdue"
            label="Valor vencido"
            secondaryFormat="number"
            secondaryLabel="ocorrências"
            secondaryValue={dashboard?.overdue.count ?? 0}
            value={dashboard?.overdue.amount ?? 0}
            valueFormat="currency"
            valueLabel="total vencido"
          />
          <OperationalIndicatorCard
            actionLabel="Ver débitos"
            accent={financeTokens.colors.accentPurple}
            description="Débitos em cobrança que precisam de acompanhamento pela última movimentação."
            href="/debts?status=InCollection&sort=stale"
            icon="stale"
            label="Cobranças sem atualização"
            secondaryLabel="valor sem atualização"
            secondaryValue={dashboard?.staleCharges.amount ?? 0}
            value={dashboard?.staleCharges.count ?? 0}
            valueLabel="débitos"
          />
        </Box>
      </Stack>
    </>
  );
}
