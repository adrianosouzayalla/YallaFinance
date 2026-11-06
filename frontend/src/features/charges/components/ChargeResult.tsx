"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { Alert, Box, Button, Divider, Stack, Typography } from "@mui/material";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { PaymentChargeSummary } from "@/features/payments/components/PaymentChargeSummary";
import type { PaymentCharge } from "@/services/api/payments/payment.types";
import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import type { Charge } from "@/services/api/charges/charge.types";
import { ChargeItemPostings, type ChargePostingItem } from "./ChargeItemPostings";
import { ChargeProcessingPanel } from "./ChargeProcessingPanel";

interface ChargeResultData {
  reference?: string;
  amount: number;
  formattedAmount?: string;
  payment: Charge["payment"];
  items?: readonly ChargePostingItem[];
  processing?: Charge["processing"];
  financialIntegration?: Charge["financialIntegration"];
  providerExternalId?: string;
  pixId?: string;
}

interface ChargeResultProps {
  charge: ChargeResultData;
  categories: readonly { id: string; name: string }[];
  categoryName?: string;
  additionalDetails?: readonly (readonly [string, string])[];
  refreshingIntegration?: boolean;
  onRefreshIntegration?: () => void;
  payment?: {
    charge?: PaymentCharge | null;
    href?: string | null;
    isFetching?: boolean;
    isError?: boolean;
    onRetry?: () => void;
  };
  actions?: ReactNode;
  showIntegrationStatus?: boolean;
}

/** Shared presentation; queries, mutations and navigation targets belong to each caller. */
export function ChargeResult({
  charge,
  categories,
  categoryName,
  additionalDetails = [],
  refreshingIntegration = false,
  onRefreshIntegration,
  payment,
  actions,
  showIntegrationStatus = true,
}: ChargeResultProps) {
  const summaryRows = [
    ["Número da cobrança", charge.reference || "—"],
    ...(!charge.items
      ? [
          ["ID externo", charge.providerExternalId],
          ["ID PIX", charge.pixId],
        ]
      : []),
    ["Valor", charge.formattedAmount ?? formatCurrency(charge.amount)],
    ["Categoria", categoryName || (charge.items ? "Por item" : "")],
    ["Vencimento", formatDate(charge.payment.dueDate)],
    ...additionalDetails,
  ];
  const canGeneratePayment = Boolean(
    payment?.href && payment.charge?.eligible && !payment.isFetching,
  );

  return (
    <>
      <Alert severity="success">Cobrança criada com sucesso.</Alert>
      {showIntegrationStatus && charge.processing && (
        <ChargeProcessingPanel
          processing={charge.processing}
          confirmedItems={charge.items?.filter((item) => item.providerExternalId).length ?? 0}
          totalItems={charge.items?.length ?? 0}
          refreshing={refreshingIntegration}
          onRefresh={onRefreshIntegration}
        />
      )}
      {showIntegrationStatus &&
        !charge.processing &&
        charge.items &&
        charge.financialIntegration?.outcome !== "Completed" && (
          <Alert severity="warning">
            A cobrança está registrada. Há itens sem confirmação externa; confira os lançamentos
            antes de tentar novamente.
          </Alert>
        )}
      <Box
        sx={{
          display: "grid",
          gap: 3,
          gridTemplateColumns: charge.items
            ? { xs: "minmax(0, 1fr)", lg: "minmax(0, 1fr) minmax(340px, 1fr)" }
            : "minmax(0, 1fr)",
        }}
      >
        <PrototypePanel title="Dados da cobrança">
          <Stack spacing={1.5} sx={{ p: 2.5 }}>
            {summaryRows.map(([label, value]) => (
              <Box
                key={label}
                sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}
              >
                <Typography color="text.secondary">{label}</Typography>
                <Typography sx={{ fontWeight: 600, textAlign: "right", overflowWrap: "anywhere" }}>
                  {value}
                </Typography>
              </Box>
            ))}
          </Stack>
        </PrototypePanel>
        {charge.items ? (
          <Box sx={{ minWidth: 0, display: "flex", "& > section": { flex: 1 } }}>
            <ChargeItemPostings charge={charge} categories={categories} />
          </Box>
        ) : null}
      </Box>
      {payment?.href && payment.isError && (
        <Alert
          severity="warning"
          action={
            payment.onRetry ? (
              <Button onClick={payment.onRetry}>Atualizar pagamento</Button>
            ) : undefined
          }
        >
          Não foi possível consultar a situação do pagamento.
        </Alert>
      )}
      {payment?.charge && <PaymentChargeSummary charge={payment.charge} />}
      {(actions || canGeneratePayment) && (
        <>
          <Divider />
          <Stack
            direction="row"
            spacing={2}
            sx={{ justifyContent: "flex-end", flexWrap: "wrap", rowGap: 2 }}
          >
            {actions}
            {canGeneratePayment && (
              <Button
                component={Link}
                href={payment!.href!}
                variant="contained"
                startIcon={<AccountBalanceWalletOutlinedIcon />}
              >
                Gerar pagamento
              </Button>
            )}
          </Stack>
        </>
      )}
    </>
  );
}
