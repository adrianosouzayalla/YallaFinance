"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { SelectablePaginatedTable } from "@/components/ui/SelectablePaginatedTable";

import { formatCurrency } from "@/utils/currency";
import { useChargeDraft } from "@/features/charges/context/ChargeDraftProvider";
import { useChargeDebts } from "@/features/charges/hooks/useChargeDebts";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { useCustomers } from "@/features/customers/hooks/useCustomers";

import { useEffect } from "react";
import { usePaymentForm } from "@/features/charges/hooks/usePaymentForm";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Alert, Box, Button, Stack, TextField, Typography } from "@mui/material";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { DateField } from "@/components/forms/DateField";
import { ReadOnlyMappingPanel } from "@/features/charges/components/ReadOnlyMappingPanel";
import { buildSelectedDebtDraft } from "@/features/charges/chargeFlow";
import { getChargeToday, validateChargeDueDate } from "@/features/charges/businessRules";
import { resolveSelectedDebts } from "@/features/debts/debtNavigation";
import type { ChargeDraft, PaymentInput } from "@/services/api/charges/charge.types";
import type { Debt } from "@/services/api/debts/debt.types";
import type { FleetMovement } from "@/services/api/customers/customer.types";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";

const selectedPayment: PaymentInput = {
  dueDate: "",
  customerNote: "",
};

type ReviewResolution =
  { ok: true; debts: Debt[]; draft: ChargeDraft } | { ok: false; message: string };

function maskTaxId(taxId: string): string {
  const digits = taxId.replace(/\D/g, "");
  return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
}

function movementFromDebt(debt: Debt): FleetMovement {
  //TODO: Adriano Usar o vínculo oficial de movimentação; o código ainda fabrica movement-{debt.id} e usa frota como centro de custo.
  return {
    id: `movement-${debt.id}`,
    tenantId: debt.tenantId,
    customerId: debt.customerId,
    occurrenceDate: debt.occurrenceDate,
    plate: debt.plate,
    contract: debt.contract,
    product: debt.product,
    fleet: debt.fleet,
    costCenter: debt.fleet,
  };
}

function debtFromStandalone(
  draft: Extract<ChargeDraft, { source: "standalone" }>,
  categoryName: string,
): Debt {
  //TODO: Adriano Substituir a projeção sintética standalone-review-debt por um contrato próprio de revisão ou projeção persistida.
  return {
    id: "standalone-review-debt",
    category: categoryName as Debt["category"],
    tenantId: draft.movement.tenantId,
    customerId: draft.customerId,
    tenantName: draft.customerName,
    contract: draft.movement.contract,
    product: draft.movement.product,
    plate: draft.movement.plate,
    fleet: draft.movement.fleet,
    occurrenceDate: draft.standalone.occurrenceDate,
    amount: draft.standalone.amount,
    status: "Open",
    categoryId: draft.standalone.categoryId ?? "",
    description: `${draft.standalone.category.toLocaleUpperCase("pt-BR")} | ${draft.standalone.descriptionDetail} | ${draft.movement.contract}`,
    source: "Cobrança avulsa",
  };
}

export function ChargeReviewClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setChargeIntent, draft: activeDraft } = useChargeDraft();
  const source = searchParams.get("source");
  const selectedIds =
    source === "selected" ? (searchParams.get("debtIds") ?? "").split(",").filter(Boolean) : [];
  const availableDebts = useChargeDebts(selectedIds).data?.items ?? [];
  const customerName =
    source === "selected" ? availableDebts[0]?.tenantName : activeDraft?.customerName;
  const customerQuery = useCustomers(customerName ?? "", Boolean(customerName));
  const categoryQuery = useCategories();
  const customers = customerQuery.data ?? [];
  const categories = categoryQuery.data ?? [];
  const { showToast } = useToast();

  useEffect(() => {
    if (customerQuery.isError)
      showToast({ message: "Não foi possível carregar os dados do cliente.", severity: "error" });
  }, [customerQuery.isError, showToast]);
  const categoryNameForDraft = (draft: ChargeDraft) => {
    if (draft.source === "standalone") {
      return categories.find((item) => item.id === draft.standalone.categoryId)?.name ?? "";
    }
    return availableDebts.find((debt) => debt.id === draft.debtIds[0])?.category ?? "";
  };

  const resolution: ReviewResolution = (() => {
    if (source === "selected") {
      const selectedResolution = resolveSelectedDebts(selectedIds, availableDebts);
      if (!selectedResolution.ok) {
        return selectedResolution;
      }

      return {
        ...selectedResolution,
        draft: buildSelectedDebtDraft(selectedResolution.debts, selectedPayment),
      };
    }

    if (source === "standalone" && activeDraft?.source === "standalone") {
      return {
        ok: true as const,
        debts: [debtFromStandalone(activeDraft, categoryNameForDraft(activeDraft))],
        draft: activeDraft,
      };
    }

    return { ok: false as const, message: "Não foi possível localizar os dados da cobrança" };
  })();

  const draftForState = resolution.ok ? resolution.draft : null;
  const initialPayment: PaymentInput = (() => {
    if (draftForState?.source === "standalone") {
      return {
        dueDate: draftForState.standalone.dueDate,
        customerNote: draftForState.standalone.customerNote,
      };
    }

    return draftForState?.payment ?? selectedPayment;
  })();
  const form = usePaymentForm(initialPayment);
  const { payment, setPayment } = form;

  if (!resolution.ok) {
    return (
      <Stack spacing={3}>
        <FinancePageTitle navigationHref="/charges/new">
          {source === "standalone" ? "Cobrança avulsa" : "Cobrança"}
        </FinancePageTitle>
        <Alert severity="error">{resolution.message}</Alert>
        <Box>
          <Button component={Link} href="/debts" variant="outlined">
            ‹ Voltar
          </Button>
        </Box>
      </Stack>
    );
  }

  const originalDraft = resolution.draft;
  const debts = resolution.debts;
  const total = debts.reduce((sum, debt) => sum + debt.amount, 0);
  const paymentDueMessage = validateChargeDueDate(payment.dueDate);
  const paymentDueError = paymentDueMessage !== undefined;
  const tenant = customers.find((candidate) => candidate.customerId === originalDraft.customerId);
  const firstDebt = debts[0];
  const movement =
    originalDraft.source === "standalone" ? originalDraft.movement : movementFromDebt(firstDebt);

  async function continueToConfirmation() {
    if (paymentDueError) {
      return;
    }

    const nextDraft: ChargeDraft =
      originalDraft.source === "selected"
        ? buildSelectedDebtDraft(debts, payment)
        : {
            ...originalDraft,
            standalone: {
              ...originalDraft.standalone,
              dueDate: payment.dueDate,
              customerNote: payment.customerNote,
            },
          };

    try {
      setChargeIntent(nextDraft);
      showToast({ message: "Dados da cobrança preparados.", severity: "success" });
      router.push("/charges/confirmation");
    } catch {
      showToast({ message: "Não foi possível salvar os dados da cobrança.", severity: "error" });
    }
  }

  return (
    <Stack spacing={3}>
      <FinancePageTitle navigationHref="/charges/new">
        {source === "standalone" ? "Cobrança avulsa" : "Cobrança"}
      </FinancePageTitle>
      <ChargeStepper activeStep={1} />
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          void form.handleSubmit(continueToConfirmation)(event);
        }}
        sx={{ display: "grid", gap: 3, gridTemplateColumns: "minmax(0, 1fr) minmax(340px, 1fr)" }}
      >
        <Stack spacing={3}>
          <PrototypePanel title="Dados do cliente (Yalla Digital)">
            <Stack spacing={1.5} sx={{ p: 2.5 }}>
              {[
                ["Locatário", tenant?.name ?? firstDebt.tenantName],
                ["CPF/CNPJ", tenant ? maskTaxId(tenant.taxId) : ""],
                ["Contrato", `${firstDebt.contract} (${firstDebt.product})`],
                ["Telefone:", tenant?.phone || "—"],
                ["Email:", tenant?.email || "—"],
              ].map(([label, value]) => (
                <Box
                  key={label}
                  sx={{
                    display: "grid",
                    gap: 2,
                    gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
                  }}
                >
                  <Typography>{label}</Typography>
                  <Typography sx={{ fontWeight: 600, textAlign: "right" }}>{value}</Typography>
                </Box>
              ))}
            </Stack>
          </PrototypePanel>
          <PrototypePanel title="Débitos incluídos na cobrança">
            <SelectablePaginatedTable
              rows={debts}
              paginationMode="client"
              showSelection={false}
              columns={[
                { id: "category", header: "Categoria", render: (debt) => debt.category },
                { id: "description", header: "Descrição", render: (debt) => debt.description },
                {
                  id: "amount",
                  header: "Valor",
                  align: "right",
                  render: (debt) => formatCurrency(debt.amount),
                },
              ]}
              getRowId={(debt) => debt.id}
              selectedRows={[]}
              onSelectionChange={() => {}}
              tableLabel="Débitos incluídos"
              headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
              pagination={{
                page: 1,
                pageSize: Math.max(1, debts.length),
                totalItems: debts.length,
                onPageChange: () => {},
              }}
            />
            <Box sx={{ display: "flex", justifyContent: "space-between", px: 2.5, py: 2 }}>
              <Typography variant="h2">Total a cobrar</Typography>
              <Typography variant="h2" sx={{ color: financeTokens.colors.header }}>
                {formatCurrency(total)}
              </Typography>
            </Box>
          </PrototypePanel>
        </Stack>
        <Stack spacing={3}>
          <PrototypePanel title="Forma de cobrança">
            <Stack spacing={2} sx={{ p: 2.5 }}>
              <DateField
                error={paymentDueError}
                helperText={paymentDueMessage}
                label="Vencimento"
                size="small"
                min={getChargeToday()}
                value={payment.dueDate}
                onChange={(value) => setPayment({ ...payment, dueDate: value ?? "" })}
              />
              <TextField
                label="Observação ao cliente"
                minRows={3}
                multiline
                placeholder="Opcional"
                size="small"
                value={payment.customerNote}
                onChange={(event) => setPayment({ ...payment, customerNote: event.target.value })}
              />
            </Stack>
          </PrototypePanel>
          <ReadOnlyMappingPanel
            category={firstDebt.category}
            description={
              firstDebt.description.includes("|")
                ? (firstDebt.description.split("|")[1]?.trim() ?? firstDebt.description)
                : firstDebt.description
            }
            movement={movement}
            variant="review"
          />
        </Stack>
        <Box
          sx={{
            alignItems: "center",
            display: "flex",
            gridColumn: "1 / -1",
            justifyContent: "space-between",
          }}
        >
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
            Revise os dados antes de gerar a cobrança.
          </Typography>
          <Stack direction="row" spacing={1.5}>
            <Button
              component={Link}
              href={source === "selected" ? "/debts" : "/charges/new"}
              variant="outlined"
            >
              ‹ Voltar
            </Button>
            <Button disabled={paymentDueError} type="submit" variant="contained">
              Gerar cobrança
            </Button>
          </Stack>
        </Box>
      </Box>
    </Stack>
  );
}
