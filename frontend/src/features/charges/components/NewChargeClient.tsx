"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";

import { useChargeDraft } from "@/features/charges/context/ChargeDraftProvider";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { useCustomerSuggestions } from "@/features/customers/hooks/useCustomerSuggestions";
import { useFleetMovements } from "@/features/customers/hooks/useFleetMovements";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Alert, Box, Button, FormHelperText, Stack, TextField, Typography } from "@mui/material";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { DateField } from "@/components/forms/DateField";
import type { Customer } from "@/services/api/customers/customer.types";
import { ReadOnlyMappingPanel } from "@/features/charges/components/ReadOnlyMappingPanel";
import { VehicleMatchPanel } from "@/features/charges/components/VehicleMatchPanel";
import { getChargeToday } from "@/features/charges/businessRules";
import { getStandaloneFieldErrors as getFieldErrors } from "@/features/charges/schemas/charge.schema";
import { useStandaloneChargeForm } from "@/features/charges/hooks/useStandaloneChargeForm";
import type { StandaloneChargeDraft } from "@/services/api/charges/charge.types";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";
import { LoadingState } from "@/components/ui/LoadingState";
import { NewChargeDebtPanel } from "./NewChargeDebtPanel";

const defaultDraft: StandaloneChargeDraft = {
  customerId: "",
  category: "",
  amount: 0,
  occurrenceDate: "",
  descriptionDetail: "Cobrança avulsa",
  dueDate: "",
  customerNote: "",
};

export function NewChargeClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setChargeIntent } = useChargeDraft();
  const [customerInput, setCustomerInput] = useState("");
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const customerQuery = useCustomerSuggestions(customerInput, selectedCustomer === null);
  const categoryQuery = useCategories();
  const customers = customerQuery.items;
  const categories = categoryQuery.data ?? [];
  const { showToast } = useToast();

  const form = useStandaloneChargeForm(defaultDraft);
  const { draft, setDraft, errors, setErrors } = form;
  const [amountValue, setAmountValue] = useState<number | null>(defaultDraft.amount);

  const movementQuery = useFleetMovements(draft.customerId, draft.occurrenceDate);
  const movements = movementQuery.data ?? [];
  const loadingMovements = movementQuery.isFetching;
  const blockingLoad = categoryQuery.isPending || loadingMovements;
  const lookupError = movementQuery.isError;

  // A escolha vale só para o locatário e a data em que foi feita; sem escolha, a única movimentação é selecionada.
  const movementScope = `${draft.customerId}|${draft.occurrenceDate}`;
  const [pickedMovement, setPickedMovement] = useState<{ scope: string; id: string }>();
  const singleMovementId = movementQuery.data?.length === 1 ? movementQuery.data[0].id : undefined;
  const pickedMovementId = pickedMovement?.scope === movementScope ? pickedMovement.id : undefined;
  const selectedMovementId = pickedMovementId ?? singleMovementId;
  const setSelectedMovementId = (id: string | undefined) =>
    setPickedMovement(id === undefined ? undefined : { scope: movementScope, id });
  useEffect(() => {
    if (customerQuery.isError)
      showToast({ message: "Não foi possível carregar os locatários.", severity: "error" });
  }, [customerQuery.isError, showToast]);
  useEffect(() => {
    if (categoryQuery.isError)
      showToast({ message: "Não foi possível carregar os catálogos.", severity: "error" });
  }, [categoryQuery.isError, showToast]);
  useEffect(() => {
    if (movementQuery.isError)
      showToast({
        message: "Não foi possível consultar a movimentação de frota.",
        severity: "error",
      });
  }, [movementQuery.isError, showToast]);

  const selectedMovement = movements.find(
    (movement) =>
      movement.id === selectedMovementId &&
      movement.customerId === draft.customerId &&
      movement.occurrenceDate === draft.occurrenceDate,
  );
  const formErrors = getFieldErrors(draft);
  const directVisitFeedback = {
    "revise-a-cobranca-primeiro": "Revise os dados da cobrança antes de confirmar.",
    "complete-a-cobranca-primeiro": "Conclua a geração da cobrança antes de acessar o envio.",
  }[searchParams.get("feedback") ?? ""];
  const canContinue =
    Object.keys(formErrors).length === 0 && selectedMovement !== undefined && !loadingMovements;
  const pendingRequirements = [
    ...Object.values(formErrors),
    ...(loadingMovements ? ["Aguarde a consulta de movimentações."] : []),
    ...(!loadingMovements && !selectedMovement && draft.customerId && draft.occurrenceDate
      ? [
          lookupError
            ? "Não foi possível consultar as movimentações."
            : movements.length > 0
              ? "Selecione a placa do veículo."
              : "Nenhuma movimentação encontrada para o locatário nessa data.",
        ]
      : []),
  ];

  const updateDraft = useCallback(
    (nextDraft: StandaloneChargeDraft) => {
      setDraft(nextDraft);
      setErrors(getFieldErrors(nextDraft));
    },
    [setDraft, setErrors],
  );

  async function continueToReview() {
    const nextErrors = getFieldErrors(draft);
    setErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0 || selectedMovement === undefined) {
      return;
    }

    const persistedDraft: StandaloneChargeDraft = {
      ...draft,
      selectedMovementId: selectedMovement.id,
    };

    try {
      setChargeIntent({
        source: "standalone",
        customerId: draft.customerId,
        customerName: selectedCustomer?.name ?? "",
        standalone: persistedDraft,
        movement: selectedMovement,
      });
      showToast({ message: "Dados preparados para revisão.", severity: "success" });
      router.push("/charges/review?source=standalone");
    } catch {
      showToast({ message: "Não foi possível salvar a cobrança.", severity: "error" });
    }
  }

  return (
    <>
      <LoadingState
        mode="blocking"
        open={blockingLoad}
        message={
          loadingMovements ? "Consultando movimentações..." : "Carregando formulário de cobrança..."
        }
      />
      <Stack spacing={3}>
        {directVisitFeedback ? <Alert severity="info">{directVisitFeedback}</Alert> : null}
        <Box>
          <FinancePageTitle navigationHref="/charges/new">Cobrança avulsa</FinancePageTitle>
          <Typography
            color="text.secondary"
            sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}
          >
            Registre um débito e siga para a revisão da cobrança.
          </Typography>
        </Box>

        <ChargeStepper activeStep={0} />

        <Box
          component="form"
          noValidate
          onSubmit={(event) => {
            void form.handleSubmit(continueToReview)(event);
          }}
          sx={{
            display: "grid",
            gap: 3,
            gridTemplateColumns: "minmax(0, 1.25fr) minmax(340px, 0.75fr)",
          }}
        >
          <Stack spacing={3}>
            <NewChargeDebtPanel
              customers={customers}
              customersSearching={customerQuery.searching}
              customersFailed={customerQuery.isError}
              selectedCustomer={selectedCustomer}
              setSelectedCustomer={setSelectedCustomer}
              customerInput={customerInput}
              setCustomerInput={setCustomerInput}
              setSelectedMovementId={setSelectedMovementId}
              draft={draft}
              updateDraft={updateDraft}
              errors={errors}
              categories={categories}
              amountValue={amountValue}
              setAmountValue={setAmountValue}
            />

            {lookupError ? (
              <Alert severity="error">
                Não foi possível consultar a movimentação de frota. Tente novamente.
              </Alert>
            ) : (
              <VehicleMatchPanel
                loading={loadingMovements}
                movements={movements}
                selectedMovementId={selectedMovementId}
                onSelect={setSelectedMovementId}
              />
            )}
          </Stack>

          <Stack spacing={3}>
            <PrototypePanel title="Forma de cobrança">
              <Stack spacing={2} sx={{ p: 2.5 }}>
                <DateField
                  error={errors.dueDate !== undefined}
                  helperText={errors.dueDate}
                  label="Vencimento"
                  size="small"
                  min={getChargeToday()}
                  value={draft.dueDate}
                  onChange={(value) => updateDraft({ ...draft, dueDate: value ?? "" })}
                />
                <TextField
                  label="Observação ao cliente"
                  minRows={3}
                  multiline
                  placeholder="Opcional"
                  size="small"
                  value={draft.customerNote}
                  onChange={(event) => updateDraft({ ...draft, customerNote: event.target.value })}
                />
              </Stack>
            </PrototypePanel>
            <ReadOnlyMappingPanel
              category={draft.category}
              description={draft.descriptionDetail}
              movement={selectedMovement}
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
            <Typography
              color="text.secondary"
              sx={{ fontSize: financeTokens.typography.secondary }}
            >
              * Campos obrigatórios.
            </Typography>
            <Stack direction="row" spacing={1.5}>
              <Button component={Link} href="/debts" variant="outlined">
                Cancelar
              </Button>
              <Button
                aria-describedby={!canContinue ? "charge-pending-requirements" : undefined}
                disabled={!canContinue}
                type="submit"
                variant="contained"
              >
                Continuar ›
              </Button>
            </Stack>
          </Box>
          {!canContinue && pendingRequirements.length > 0 ? (
            <Alert id="charge-pending-requirements" severity="info" sx={{ gridColumn: "1 / -1" }}>
              Para continuar: {pendingRequirements.join("; ")}
            </Alert>
          ) : null}
        </Box>
        {Object.keys(errors).length > 0 ? (
          <FormHelperText error>Revise os campos obrigatórios.</FormHelperText>
        ) : null}
      </Stack>
    </>
  );
}
