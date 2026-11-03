"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";

import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import { useChargeDraft } from "@/features/charges/context/ChargeDraftProvider";
import { useChargeDebts } from "@/features/charges/hooks/useChargeDebts";
import { useCreateCharge } from "@/features/charges/hooks/useCreateCharge";
import { ChargeIntegrationError } from "@/features/charges/charge-integration-error";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { ReadOnlyMappingPanel } from "@/features/charges/components/ReadOnlyMappingPanel";
import { createIdempotencyKey } from "@/features/charges/chargeFlow";
import { validateChargeDueDate } from "@/features/charges/businessRules";
import type { ChargeDraft } from "@/features/charges/types/charge.types";
import type { Debt } from "@/features/debts/types/debt.types";
import type { FleetMovement } from "@/features/customers/types/customer.types";
import { useToast } from "@/providers/ToastProvider";
import { LoadingState } from "@/components/ui/LoadingState";

const emptyDebts: Debt[] = [];

function draftPayment(draft: ChargeDraft) {
  return draft.source === "selected"
    ? draft.payment
    : {
        dueDate: draft.standalone.dueDate,
        customerNote: draft.standalone.customerNote,
      };
}

function amountForDraft(draft: ChargeDraft, debts: Debt[]) {
  return draft.source === "selected"
    ? debts
        .filter((debt) => draft.debtIds.includes(debt.id))
        .reduce((sum, debt) => sum + debt.amount, 0)
    : draft.standalone.amount;
}

function movementForDraft(draft: ChargeDraft, debt?: Debt): FleetMovement {
  //TODO: Adriano Usar o vínculo oficial de movimentação; o código ainda deriva os dados do débito e fabrica o identificador.
  return draft.source === "standalone"
    ? draft.movement
    : {
        id: `movement-${debt?.id ?? "selected"}`,
        tenantId: debt?.tenantId ?? "",
        customerId: draft.customerId,
        occurrenceDate: debt?.occurrenceDate ?? "",
        plate: debt?.plate ?? "",
        contract: debt?.contract ?? "",
        product: debt?.product ?? "",
        fleet: debt?.fleet ?? "",
        costCenter: debt?.fleet ?? "",
      };
}

function reviewHrefForDraft(draft: ChargeDraft): string {
  if (draft.source === "standalone") {
    return "/charges/review?source=standalone";
  }

  return `/charges/review?source=selected&debtIds=${[...draft.debtIds].sort().join(",")}`;
}

export function ChargeConfirmationClient() {
  const router = useRouter();
  const { draft } = useChargeDraft();
  const availableDebts = useChargeDebts(draft?.source === "selected" ? draft.debtIds : []).data?.items ?? emptyDebts;
  const creation = useCreateCharge();
  const { showToast } = useToast();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitted = useRef(false);

  const selectedDebts = useMemo(
    () =>
      draft?.source === "selected"
        ? availableDebts.filter((debt) => draft.debtIds.includes(debt.id))
        : [],
    [draft, availableDebts],
  );

  useEffect(() => {
    if (draft === null) {
      router.replace("/charges/new?feedback=revise-a-cobranca-primeiro");
    }
  }, [draft, router]);

  if (draft === null) {
    return null;
  }

  const currentDraft: ChargeDraft = draft;

  const payment = draftPayment(currentDraft);
  const amount = amountForDraft(currentDraft, availableDebts);
  const debt = selectedDebts[0];
  const customerName = currentDraft.customerName;
  const description =
    currentDraft.source === "selected"
      ? (debt?.description ?? "")
      : currentDraft.standalone.descriptionDetail;

  async function confirm() {
    if (pending || submitted.current) {
      return;
    }

    submitted.current = true;
    setPending(true);
    setError(null);
    try {
      const dueDateError = validateChargeDueDate(draftPayment(currentDraft).dueDate);
      if (dueDateError) throw new Error(dueDateError);
      const charge = await creation.mutateAsync({
        draft: currentDraft,
        idempotencyKey: createIdempotencyKey(currentDraft),
      });
      showToast({
        message:
          charge.processing?.status === "Pending"
            ? "Cobrança criada; integração agendada."
            : "Cobrança registrada.",
        severity: "success",
      });
      router.push(`/charges/success?chargeId=${charge.id}`);
    } catch (cause) {
      submitted.current = false;
      const message = cause instanceof Error ? cause.message : "Não foi possível gerar a cobrança.";
      setError(message);
      setPending(false);
      showToast({
        message,
        severity: "error",
      });
    }
  }

  const summaryRows = [
    ["Locatário", customerName],
    [
      "Débitos incluídos",
      String(currentDraft.source === "selected" ? currentDraft.debtIds.length : 1),
    ],
    ["Total a cobrar", formatCurrency(amount)],
    ["Vencimento", formatDate(payment.dueDate)],
    ["Observação ao cliente", payment.customerNote || "Opcional"],
  ];

  return (
    <>
    <LoadingState mode="blocking" open={pending} message="Gerando cobrança..." />
    <Stack spacing={3}>
      <FinancePageTitle
        navigationHref="/charges/new"
      >
        Confirmar cobrança
      </FinancePageTitle>
      <ChargeStepper activeStep={2} />
      {error ? (
        <Alert severity="error">
          {error}
          {creation.error instanceof ChargeIntegrationError ? (
            <Typography variant="body2">Número da cobrança: {creation.error.charge.reference}</Typography>
          ) : null}
        </Alert>
      ) : null}
      <Box
        sx={{ display: "grid", gap: 3, gridTemplateColumns: "minmax(0, 1fr) minmax(340px, 1fr)" }}
      >
        <PrototypePanel title="Resumo da cobrança">
          <Stack spacing={1.5} sx={{ p: 2.5 }}>
            {summaryRows.map(([label, value]) => (
              <Box
                key={label}
                sx={{
                  display: "grid",
                  gap: 2,
                  gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
                }}
              >
                <Typography color="text.secondary">{label}</Typography>
                <Typography
                  sx={{ fontWeight: label === "Total a cobrar" ? 700 : 500, textAlign: "right" }}
                >
                  {value}
                </Typography>
              </Box>
            ))}
          </Stack>
        </PrototypePanel>
        <ReadOnlyMappingPanel
          category={
            currentDraft.source === "selected"
              ? (debt?.category ?? "")
              : currentDraft.standalone.category
          }
          description={description}
          movement={movementForDraft(currentDraft, debt)}
          variant="review"
        />
      </Box>
      <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
        <Stack direction="row" spacing={1.5}>
          <Button component={Link} href={reviewHrefForDraft(draft)} variant="outlined">
            ‹ Voltar
          </Button>
          <Button disabled={pending} onClick={() => void confirm()} variant="contained">
            {pending ? "Gerando cobrança..." : "Gerar cobrança"}
          </Button>
        </Stack>
      </Box>
    </Stack>
    </>
  );
}
