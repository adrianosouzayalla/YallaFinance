"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";

import { useChargeDraft } from "@/features/charges/context/ChargeDraftProvider";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { useCustomerSuggestions } from "@/features/customers/hooks/useCustomerSuggestions";
import { useFleetMovements } from "@/features/customers/hooks/useFleetMovements";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import {
  Alert,
  Box,
  Button,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import { AutocompleteField } from "@/components/forms/AutocompleteField";
import type { Customer } from "@/features/customers/types/customer.types";
import { ReadOnlyMappingPanel } from "@/features/charges/components/ReadOnlyMappingPanel";
import { VehicleMatchPanel } from "@/features/charges/components/VehicleMatchPanel";
import { getChargeToday } from "@/features/charges/businessRules";
import { getStandaloneFieldErrors as getFieldErrors } from "@/features/charges/schemas/charge.schema";
import { useStandaloneChargeForm } from "@/features/charges/hooks/useStandaloneChargeForm";
import type { DebtCategory } from "@/features/debts/types/debt.types";
import type { StandaloneChargeDraft } from "@/features/charges/types/charge.types";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";
import { LoadingState } from "@/components/ui/LoadingState";

const defaultDraft: StandaloneChargeDraft = {
  customerId: "",
  category: "",
  amount: 0,
  occurrenceDate: "",
  descriptionDetail: "Cobrança avulsa",
  dueDate: "",
  customerNote: "",
};

function maskTaxId(taxId: string): string {
  const digits = taxId.replace(/\D/g, "");
  return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
}

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

  const [selectedMovementId, setSelectedMovementId] = useState<string>();

  const movementQuery = useFleetMovements(draft.customerId, draft.occurrenceDate);
  const movements = movementQuery.data ?? [];
  const loadingMovements = movementQuery.isFetching;
  const blockingLoad = categoryQuery.isPending || loadingMovements;
  const lookupError = movementQuery.isError;
  useEffect(() => {
    void Promise.resolve().then(() => {
      setSelectedMovementId(
        movementQuery.data?.length === 1 ? movementQuery.data[0].id : undefined,
      );
    });
  }, [movementQuery.data, draft.customerId, draft.occurrenceDate]);
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
    <LoadingState mode="blocking" open={blockingLoad} message={loadingMovements ? "Consultando movimentações..." : "Carregando formulário de cobrança..."} />
    <Stack spacing={3}>
      {directVisitFeedback ? <Alert severity="info">{directVisitFeedback}</Alert> : null}
      <Box>
        <FinancePageTitle
          navigationHref="/charges/new"
        >
          Cobrança avulsa
        </FinancePageTitle>
        <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}>
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
          <PrototypePanel title="Débito a cobrar">
            <Stack spacing={2} sx={{ p: 2.5 }}>
              <AutocompleteField<Customer>
                label="Locatário"
                options={customers}
                value={selectedCustomer}
                inputValue={customerInput}
                getOptionKey={(customer) => customer.customerId}
                getOptionLabel={(customer) => `${customer.name} (${maskTaxId(customer.taxId)})`}
                onChange={(customer) => {
                  setSelectedCustomer(customer);
                  setCustomerInput(customer ? `${customer.name} (${maskTaxId(customer.taxId)})` : "");
                  setSelectedMovementId(undefined);
                  updateDraft({ ...draft, customerId: customer?.customerId ?? "", selectedMovementId: undefined });
                }}
                onInputChange={(text) => {
                  setCustomerInput(text);
                  if (selectedCustomer) {
                    setSelectedCustomer(null);
                    setSelectedMovementId(undefined);
                    updateDraft({ ...draft, customerId: "", selectedMovementId: undefined });
                  }
                }}
                loading={customerQuery.searching}
                error={Boolean(errors.customerId) || customerQuery.isError}
                helperText={errors.customerId ?? (customerQuery.isError
                  ? "Não foi possível buscar os locatários. Tente novamente."
                  : selectedCustomer ? undefined : "Digite pelo menos 3 caracteres do nome ou documento")}
                placeholder="Nome ou CPF/CNPJ"
                noOptionsText={customerInput.trim().length < 3
                  ? "Digite pelo menos 3 caracteres" : "Nenhum locatário encontrado"}
              />
              <FormControl fullWidth size="small">
                <InputLabel id="category-label">Categoria</InputLabel>
                <Select
                  label="Categoria"
                  labelId="category-label"
                  value={draft.categoryId ?? ""}
                  onChange={(event) => {
                    const selected = categories.find((item) => item.id === event.target.value);
                    updateDraft({
                      ...draft,
                      category: (selected?.name ?? "") as DebtCategory,
                      categoryId: selected?.id,
                    });
                  }}
                >
                  <MenuItem value="">Selecione uma categoria</MenuItem>
                  {categories.map((category) => (
                    <MenuItem key={category.id} value={category.id}>
                      {category.name}
                    </MenuItem>
                  ))}
                </Select>
                {errors.categoryId ? (
                  <FormHelperText error>{errors.categoryId}</FormHelperText>
                ) : null}
              </FormControl>
              <Box
                sx={{
                  display: "grid",
                  gap: 2,
                  gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
                }}
              >
                <CurrencyField
                  error={errors.amount !== undefined}
                  helperText={errors.amount}
                  label="Valor"
                  size="small"
                  value={amountValue}
                  onChange={(value) => {
                    setAmountValue(value);
                    updateDraft({ ...draft, amount: value ?? 0 });
                  }}
                />
                <DateField
                  error={errors.occurrenceDate !== undefined}
                  helperText={errors.occurrenceDate}
                  label="Data da ocorrência"
                  size="small"
                  value={draft.occurrenceDate}
                  onChange={(value) => updateDraft({ ...draft, occurrenceDate: value ?? "" })}
                />
              </Box>
              <TextField
                error={errors.descriptionDetail !== undefined}
                helperText={errors.descriptionDetail}
                label="Detalhe da descrição"
                required
                size="small"
                value={draft.descriptionDetail}
                onChange={(event) =>
                  updateDraft({ ...draft, descriptionDetail: event.target.value })
                }
                onBlur={() => {
                  if (!draft.descriptionDetail.trim()) {
                    updateDraft({ ...draft, descriptionDetail: "Cobrança avulsa" });
                  }
                }}
              />
              <Box>
                <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary, mb: 0.75 }}>
                  Anexo / evidência
                </Typography>
                {/*TODO: Adriano Implementar o upload e a persistência de anexos/evidências; o campo atual é apenas visual.*/}
                <Box
                  sx={{
                    border: "1px dashed",
                    borderColor: financeTokens.colors.border,
                    borderRadius: `${financeTokens.radius.control}px`,
                    color: financeTokens.colors.mutedText,
                    px: 2,
                    py: 1.5,
                  }}
                >
                  Arraste fotos, laudo ou auto de infração
                </Box>
              </Box>
            </Stack>
          </PrototypePanel>

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
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
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
