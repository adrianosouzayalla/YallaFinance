"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { financeTokens } from "@/theme/designTokens";

import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import { useDebt } from "@/features/debts/hooks/useDebt";

import { useEffect } from "react";
import Link from "next/link";
import { Alert, Box, Button, Divider, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { StatusBadge } from "@/features/debts/components/StatusBadge";
import { buildReviewUrl } from "@/features/debts/debtNavigation";
import { useToast } from "@/providers/ToastProvider";
import { LoadingState } from "@/components/ui/LoadingState";

interface DebtDetailClientProps {
  debtId: string;
}

export function DebtDetailClient({ debtId }: DebtDetailClientProps) {
  const query = useDebt(debtId);
  const debt = query.isError ? null : query.data;
  const error = query.isError;
  const { showToast } = useToast();

  useEffect(() => {
    if (query.isError)
      showToast({ message: "Não foi possível consultar este débito.", severity: "error" });
  }, [query.isError, showToast]);

  if (query.isFetching && !query.data) {
    return <LoadingState mode="blocking" message="Consultando débito..." />;
  }

  if (debt === undefined) {
    return <LoadingState mode="blocking" message="Consultando débito..." />;
  }

  if (debt === null) {
    return (
      <Stack spacing={2} sx={{ alignItems: "center", p: 4 }}>
        <FinancePageTitle navigationHref="/debts" variant="h4">
          Débito não encontrado
        </FinancePageTitle>
        <Typography color="text.secondary">
          {error
            ? "Não foi possível consultar este débito."
            : "O débito solicitado não está disponível."}
        </Typography>
        <Button component={Link} href="/debts" variant="contained">
          Voltar para consulta de débitos
        </Button>
        {error ? <Button onClick={() => void query.refetch()}>Tentar novamente</Button> : null}
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
        <Box>
          <FinancePageTitle
            navigationHref="/debts"
          >
            Detalhe do débito
          </FinancePageTitle>
          <Typography color="text.secondary">Referência: {debt.id}</Typography>
        </Box>
        <Stack direction="row" spacing={1.25}>
          <Button component={Link} href="/debts" sx={{ minWidth: 140 }} variant="outlined">
            Voltar para consulta de débitos
          </Button>
          {debt.status === "Open" ? (
            <Button
              component={Link}
              href={buildReviewUrl([debt])}
              sx={{ minWidth: 140 }}
              variant="contained"
            >
              Cobrar débito
            </Button>
          ) : null}
        </Stack>
      </Stack>

      <PrototypePanel title="Dados do débito">
        <Stack divider={<Divider flexItem />} spacing={0} sx={{ p: 2.5 }}>
          <DetailRow label="Categoria" value={debt.category} />
          <DetailRow label="Valor" value={formatCurrency(debt.amount)} />
          <DetailRow label="Data de origem" value={formatDate(debt.occurrenceDate)} />
          <DetailRow label="Status" value={<StatusBadge status={debt.status} />} />
        </Stack>
      </PrototypePanel>

      <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
        <PrototypePanel title="Origem e descrição">
          <Stack spacing={1.5} sx={{ p: 2.5 }}>
            <DetailRow label="Origem" value={debt.source} />
          <DetailRow label="Descrição" value={debt.description} />
          </Stack>
        </PrototypePanel>
        <PrototypePanel title="Locatário e contrato">
          <Stack spacing={1.5} sx={{ p: 2.5 }}>
            <DetailRow label="Locatário" value={debt.tenantName} />
            <DetailRow label="Contrato" value={`${debt.contract} (${debt.product})`} />
            <DetailRow label="Veículo" value={debt.plate} />
            <DetailRow label="Frota" value={debt.fleet} />
          </Stack>
        </PrototypePanel>
        <PrototypePanel title="Evidências">
          {/*TODO: Adriano Carregar evidências persistidas e implementar upload/download autorizado; hoje há somente placeholder.*/}
          <Typography sx={{ p: 2.5 }}>Nenhuma evidência anexada.</Typography>
        </PrototypePanel>
        <PrototypePanel title="Integração financeira">
          <Stack spacing={1.5} sx={{ p: 2.5 }}>
            <DetailRow label="Centro de custo" value={debt.fleet} />
            <DetailRow
              label="Referência externa"
              value={debt.providerExternalId || "Sem referência externa confirmada"}
            />
            {debt.chargeId ? (
              <Button
                component={Link}
                href={`/charges/success?chargeId=${encodeURIComponent(debt.chargeId)}`}
                variant="outlined"
              >
                Ver cobrança vinculada
              </Button>
            ) : null}
          </Stack>
        </PrototypePanel>
      </Box>

      <PrototypePanel title="Linha do tempo">
        <Alert severity="info" sx={{ m: 2.5 }}>
          Débito registrado no Yalla Finance em {formatDate(debt.occurrenceDate)}.
        </Alert>
      </PrototypePanel>
    </Stack>
  );
}

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Stack
      direction="row"
      spacing={2}
      sx={{ alignItems: "center", justifyContent: "space-between", py: 0.75 }}
    >
      <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
        {label}
      </Typography>
      <Typography component="div" sx={{ fontSize: financeTokens.typography.body, fontWeight: 600 }}>
        {value}
      </Typography>
    </Stack>
  );
}
