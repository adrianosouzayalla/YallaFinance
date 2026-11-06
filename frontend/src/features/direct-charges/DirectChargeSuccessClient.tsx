import { useEffect, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Box, Button, Stack } from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { useToast } from "@/providers/ToastProvider";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { LoadingState } from "@/components/ui/LoadingState";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { ChargeResult } from "@/features/charges/components/ChargeResult";
import { chargesApi } from "@/services/api/charges/chargesApi.client";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { ApiError } from "@/services/api/apiError";
import { formatPlate } from "@/utils/plate";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { stepIcons, stepLabels } from "./directChargeLayout";
import { buttonRow, fieldGap } from "@/theme/layout";

export function DirectChargeSuccessClient({ chargeId }: { chargeId: string | null }) {
  const { tenantId, contextId } = useSessionContext();
  const { showToast } = useToast();
  const categoriesQuery = useCategories();
  const [refreshing, setRefreshing] = useState(false);
  const validId = Boolean(
    chargeId && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(chargeId),
  );
  const chargeQuery = useQuery({
    queryKey: ["charge", tenantId, contextId, chargeId],
    enabled: validId,
    staleTime: 60_000,
    retry: false,
    queryFn: ({ signal }) => chargesApi.refreshDirectChargeStatus(chargeId!, { contextId, signal }),
  });
  useEffect(() => {
    if (!validId || chargeQuery.isError) {
      showToast({
        severity: "error",
        message: !validId
          ? "Identificador da cobrança inválido."
          : chargeQuery.error instanceof ApiError
            ? chargeQuery.error.message
            : "Não foi possível carregar a cobrança. Tente novamente.",
      });
    }
  }, [validId, chargeQuery.isError, chargeQuery.error, showToast]);

  async function refreshCharge() {
    if (refreshing) return;
    setRefreshing(true);
    try {
      const result = await chargeQuery.refetch();
      if (!result.isError)
        showToast({ message: "Situação da cobrança atualizada.", severity: "success" });
    } finally {
      setRefreshing(false);
    }
  }

  const charge = chargeQuery.data;
  const debt = charge?.items?.[0]?.debt;
  return (
    <>
      <LoadingState
        mode="blocking"
        open={(validId && chargeQuery.isPending) || categoriesQuery.isPending || refreshing}
        message="Carregando cobrança..."
      />
      <Stack spacing={fieldGap}>
        <FinancePageTitle navigationHref="/direct-charges/new">Cobrança Direta</FinancePageTitle>
        <ChargeStepper
          activeStep={3}
          steps={stepLabels}
          icons={stepIcons}
          ariaLabel="Etapas da Cobrança Direta"
          responsive
        />
        {charge && (
          <ChargeResult
            charge={charge}
            categories={categoriesQuery.data ?? []}
            additionalDetails={[
              ["Cliente", charge.customerName || "—"],
              ["Contrato", debt?.contract ?? "—"],
              ["Placa", debt?.plate ? formatPlate(debt.plate) : "—"],
              ["Centro de custo", charge.items?.[0]?.costCenterName ?? "—"],
              ...(charge.payment.customerNote?.trim()
                ? [["Observação ao cliente", charge.payment.customerNote] as const]
                : []),
            ]}
            actions={
              <>
                <Button
                  variant="outlined"
                  startIcon={<RefreshOutlinedIcon />}
                  onClick={() => void refreshCharge()}
                  disabled={refreshing}
                >
                  Atualizar situação
                </Button>
                <Button
                  component={Link}
                  href="/direct-charges/new"
                  variant="contained"
                  startIcon={<AddOutlinedIcon />}
                >
                  Nova Cobrança Direta
                </Button>
              </>
            }
          />
        )}
        {!charge && !chargeQuery.isFetching && (
          <Box sx={buttonRow}>
            {validId && (
              <Button variant="outlined" onClick={() => void refreshCharge()}>
                Tentar novamente
              </Button>
            )}
            <Button component={Link} href="/direct-charges/new" variant="contained">
              Nova Cobrança Direta
            </Button>
          </Box>
        )}
      </Stack>
    </>
  );
}
