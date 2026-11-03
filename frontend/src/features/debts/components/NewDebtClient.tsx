"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { financeTokens } from "@/theme/designTokens";
import { useEffect, useRef, useState } from "react";
import Link from "next/link";
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
import { useForm, useWatch, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useCustomerSuggestions } from "@/features/customers/hooks/useCustomerSuggestions";
import { useCustomerContracts } from "@/features/customers/hooks/useCustomerContracts";
import { useFleetMovements } from "@/features/customers/hooks/useFleetMovements";
import { isValidDateOnly } from "@/utils/date";
import type { Customer } from "@/features/customers/types/customer.types";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { AutocompleteField } from "@/components/forms/AutocompleteField";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { ApiError } from "@/services/api/api-client";
import { useCreateManualDebt } from "../hooks/useCreateManualDebt";
import { manualDebtSchema } from "../schemas/manual-debt.schema";
import type { ManualDebtForm, ManualDebtInput } from "../types/manual-debt.types";
import { readManualDebtAttempt, storeManualDebtAttempt } from "../manualDebtAttempt";
import { LoadingState } from "@/components/ui/LoadingState";
import { createSecureUuidV4 } from "@/utils/secureUuid";

const defaults: ManualDebtForm = {
  customerId: "",
  contractId: "",
  categoryId: "",
  amount: "",
  occurrenceDate: "",
  dueDate: "",
  description: "",
};
function customerLabel(customer: Customer) {
  const digits = customer.taxId.replace(/\D/g, "");
  return `${customer.name} (${digits.length === 14 ? "CNPJ" : "CPF"} •••${digits.slice(-4)})`;
}

export function NewDebtClient() {
  const { tenantId, contextId } = useSessionContext();
  const [ready, setReady] = useState(false);
  useEffect(() => {
    let active = true;
    void Promise.resolve().then(() => {
      if (active) setReady(true);
    });
    return () => {
      active = false;
    };
  }, []);
  if (!ready) return <LoadingState mode="blocking" message="Preparando formulário..." />;
  return (
    <ManualDebtFormClient
      key={JSON.stringify([tenantId, contextId])}
      scope={JSON.stringify([tenantId, contextId])}
    />
  );
}

function ManualDebtFormClient({ scope }: { scope: string }) {
  const attemptStorageKey = `manual-debt-attempt:${scope}`;
  const [attempt, setAttempt] = useState<ManualDebtInput | null>(() =>
    readManualDebtAttempt(attemptStorageKey),
  );
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [uncertain, setUncertain] = useState(Boolean(attempt));
  const inFlight = useRef(false);
  const mounted = useRef(true);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);
  const mutation = useCreateManualDebt();
  const form = useForm<ManualDebtForm>({
    resolver: zodResolver(manualDebtSchema),
    defaultValues: attempt ?? defaults,
  });
  const {
    control,
    register,
    setValue,
    reset,
    formState: { errors },
  } = form;
  const customerId = useWatch({ control, name: "customerId" });
  const contractId = useWatch({ control, name: "contractId" });
  const occurrenceDate = useWatch({ control, name: "occurrenceDate" });
  const validOccurrence = isValidDateOnly(occurrenceDate);
  const suggestions = useCustomerSuggestions(search, !customer && !uncertain, "persisted");
  const movements = useFleetMovements(customerId, validOccurrence ? occurrenceDate : "");
  const contracts = useCustomerContracts(customerId, validOccurrence ? occurrenceDate : "");
  const categories = useCategories();
  const contractItems = validOccurrence
    ? (contracts.data?.pages.flatMap((page) => page.items) ?? []).filter((contract) =>
        movements.data?.some(
          (movement) =>
            movement.customerId === customerId &&
            movement.occurrenceDate === occurrenceDate &&
            movement.contract === contract.number &&
            movement.plate === (contract.plate ?? "") &&
            movement.product === (contract.product ?? "") &&
            movement.fleet === (contract.fleet ?? ""),
        ),
      )
    : [];
  const selectedContract = contractItems.find((item) => item.id === contractId);
  const locked = uncertain || mutation.isPending;

  function changeCustomer(value: Customer | null) {
    setCustomer(value);
    setSearch(value ? customerLabel(value) : "");
    setValue("customerId", value?.customerId ?? "", { shouldValidate: true });
    setValue("contractId", "", { shouldValidate: false });
  }

  async function save(values: ManualDebtForm) {
    if (inFlight.current) return;
    inFlight.current = true;
    setError("");
    setSuccess(false);
    const input = attempt ?? { ...values, idempotencyKey: createSecureUuidV4() };
    let sent = false;
    try {
      // Store before sending so a reload after a lost response can safely replay.
      storeManualDebtAttempt(attemptStorageKey, input);
      setAttempt(input);
      sent = true;
      await mutation.mutateAsync(input);
      storeManualDebtAttempt(attemptStorageKey, null);
      if (mounted.current) {
        reset(defaults);
        setCustomer(null);
        setSearch("");
        setAttempt(null);
        setUncertain(false);
        setSuccess(true);
      }
    } catch (failure) {
      if (!mounted.current) return;
      if (!sent) {
        setError(
          "Não foi possível preparar o envio neste navegador. Verifique o armazenamento da sessão e tente novamente.",
        );
        return;
      }
      const definitive =
        failure instanceof ApiError &&
        failure.status >= 400 &&
        failure.status < 500 &&
        failure.status !== 408 &&
        failure.status !== 429 &&
        failure.status !== 409;
      if (definitive) {
        storeManualDebtAttempt(attemptStorageKey, null);
        setAttempt(null);
        setUncertain(false);
        setError(
          "Não foi possível salvar. Confira os dados e a disponibilidade do cliente, contrato e categoria.",
        );
      } else {
        setUncertain(true);
        setError(
          failure instanceof ApiError && failure.status === 409
            ? "A tentativa conflita com um registro existente. Confira o lançamento antes de iniciar outra tentativa."
            : "Não foi possível confirmar a gravação. Confirme esta tentativa antes de alterar os dados.",
        );
      }
    } finally {
      inFlight.current = false;
    }
  }

  const blockingLoad = categories.isPending || mutation.isPending || contracts.isFetchingNextPage ||
    (customerId !== "" && (contracts.isFetching || movements.isFetching));
  return (
    <>
    <LoadingState mode="blocking" open={blockingLoad} message={mutation.isPending ? "Salvando débito..." : contracts.isFetchingNextPage ? "Carregando mais contratos..." : "Carregando dados do débito..."} />
    <Stack spacing={3}>
      <Box>
        <FinancePageTitle
          navigationHref="/debts/new"
        >
          Débito avulso
        </FinancePageTitle>
        <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}>
          Registre um débito manual em aberto.
        </Typography>
      </Box>
      {error ? <Alert severity="error">{error}</Alert> : null}
      {success ? <Alert severity="success">Débito cadastrado em aberto.</Alert> : null}
      {uncertain && !error ? (
        <Alert severity="info">
          Existe uma tentativa sem confirmação. Confirme esta tentativa antes de alterar os dados.
        </Alert>
      ) : null}
      <Box
        component="form"
        sx={(theme) => ({
          width: "100%",
          maxWidth: { md: `calc(1036px + ${theme.spacing(5)} + 2px)` },
        })}
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (uncertain && attempt) void save(attempt);
          else void form.handleSubmit(save)(event);
        }}
      >
        <PrototypePanel title="Dados do débito">
          <Stack spacing={2} sx={{ p: 2.5 }}>
            <Box
              sx={{
                display: "grid",
                gap: 2,
                gridTemplateColumns: {
                  xs: "1fr",
                  md: "minmax(0, 480px) minmax(0, 240px) minmax(0, 300px)",
                },
                width: "100%",
                maxWidth: { md: 1036 },
                alignItems: "start",
              }}
            >
              <AutocompleteField<Customer>
                label="Locatário"
                required
                disabled={locked}
                options={suggestions.items}
                value={customer}
                inputValue={search}
                getOptionKey={(item) => item.customerId}
                getOptionLabel={customerLabel}
                onChange={changeCustomer}
                onInputChange={(text) => {
                  setSearch(text);
                  if (customer) {
                    setCustomer(null);
                    setValue("customerId", "");
                    setValue("contractId", "");
                  }
                }}
                loading={suggestions.searching}
                error={Boolean(errors.customerId) || suggestions.isError}
                helperText={
                  errors.customerId?.message ??
                  (suggestions.isError
                    ? "Não foi possível consultar os clientes. Tente novamente."
                    : customer
                      ? undefined
                      : "Digite pelo menos 3 caracteres do nome ou documento")
                }
                noOptionsText={
                  search.trim().length < 3
                    ? "Digite pelo menos 3 caracteres"
                    : "Nenhum cliente encontrado"
                }
              />
              <Controller
                name="occurrenceDate"
                control={control}
                render={({ field }) => (
                  <DateField
                    label="Data da ocorrência"
                    required
                    size="small"
                    disabled={locked}
                    value={field.value}
                    onChange={(value) => {
                      field.onChange(value ?? "");
                      setValue("contractId", "");
                    }}
                    error={Boolean(errors.occurrenceDate)}
                    helperText={errors.occurrenceDate?.message}
                  />
                )}
              />
              <Controller
                name="contractId"
                control={control}
                render={({ field }) => (
                  <FormControl
                    fullWidth
                    size="small"
                    required
                    error={Boolean(errors.contractId)}
                    disabled={
                      locked ||
                      !customerId ||
                      !validOccurrence ||
                      contracts.isPending ||
                      movements.isPending ||
                      movements.isError
                    }
                  >
                    <InputLabel id="manual-contract-label">Contrato / veículo</InputLabel>
                    <Select {...field} label="Contrato / veículo" labelId="manual-contract-label">
                      <MenuItem value="">Selecione um contrato</MenuItem>
                      {contractItems.map((item) => (
                        <MenuItem key={item.id} value={item.id}>
                          {item.number} — {item.plate ?? "Sem placa"}
                        </MenuItem>
                      ))}
                    </Select>
                    <FormHelperText>
                      {errors.contractId?.message ??
                        (customerId &&
                        validOccurrence &&
                        !movements.isPending &&
                        !movements.isError &&
                        !contracts.isPending &&
                        contractItems.length === 0 &&
                        !contracts.isError
                          ? "Nenhum contrato disponível para a ocorrência nas páginas carregadas."
                          : undefined)}
                    </FormHelperText>
                  </FormControl>
                )}
              />
            </Box>
            {movements.isError ? (
              <Alert
                severity="error"
                action={<Button onClick={() => void movements.refetch()}>Tentar novamente</Button>}
              >
                Não foi possível consultar a movimentação na data informada.
              </Alert>
            ) : null}
            {contracts.isError ? (
              <Alert
                severity="error"
                action={<Button onClick={() => void contracts.refetch()}>Tentar novamente</Button>}
              >
                Não foi possível consultar os contratos.
              </Alert>
            ) : null}
            {contracts.hasNextPage ? (
              <Button
                disabled={contracts.isFetchingNextPage || locked}
                onClick={() => void contracts.fetchNextPage()}
              >
                Carregar mais contratos
              </Button>
            ) : null}
            {selectedContract ? (
              <Typography color="text.secondary" variant="body2">
                Produto: {selectedContract.product ?? "—"} · Frota: {selectedContract.fleet ?? "—"}
              </Typography>
            ) : null}
            {categories.isError ? (
              <Alert
                severity="error"
                action={<Button onClick={() => void categories.refetch()}>Tentar novamente</Button>}
              >
                Não foi possível consultar as categorias.
              </Alert>
            ) : null}
            <Box
              sx={{
                display: "grid",
                gap: 2,
                gridTemplateColumns: {
                  xs: "1fr",
                  md: "minmax(0, 480px) minmax(0, 240px) minmax(0, 300px)",
                },
                width: "100%",
                maxWidth: { md: 1036 },
              }}
            >
              <Controller
                name="categoryId"
                control={control}
                render={({ field }) => (
                  <FormControl
                    fullWidth
                    size="small"
                    required
                    error={Boolean(errors.categoryId)}
                    disabled={locked || categories.isPending}
                  >
                    <InputLabel id="manual-category-label">Categoria</InputLabel>
                    <Select {...field} label="Categoria" labelId="manual-category-label">
                      <MenuItem value="">Selecione uma categoria</MenuItem>
                      {(categories.data ?? []).map((item) => (
                        <MenuItem key={item.id} value={item.id}>
                          {item.name}
                        </MenuItem>
                      ))}
                    </Select>
                    <FormHelperText>{errors.categoryId?.message}</FormHelperText>
                  </FormControl>
                )}
              />
              <Controller
                name="amount"
                control={control}
                render={({ field }) => (
                  <CurrencyField
                    exactDecimal
                    label="Valor"
                    required
                    size="small"
                    disabled={locked}
                    value={field.value || null}
                    onChange={(value) => field.onChange(value ?? "")}
                    error={Boolean(errors.amount)}
                    helperText={errors.amount?.message}
                  />
                )}
              />
              <Controller
                name="dueDate"
                control={control}
                render={({ field }) => (
                  <DateField
                    label="Vencimento"
                    required
                    size="small"
                    disabled={locked}
                    value={field.value}
                    onChange={(value) => field.onChange(value ?? "")}
                    error={Boolean(errors.dueDate)}
                    helperText={errors.dueDate?.message}
                  />
                )}
              />
            </Box>
            <TextField
              {...register("description")}
              sx={{ width: "100%", maxWidth: { md: 1036 } }}
              label="Descrição"
              required
              size="small"
              multiline
              minRows={2}
              disabled={locked}
              error={Boolean(errors.description)}
              helperText={errors.description?.message}
            />
            <Typography color="text.secondary" variant="body2">
              Status inicial: Em aberto
            </Typography>
          </Stack>
        </PrototypePanel>
        <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end", mt: 3 }}>
          <Button component={Link} href="/debts" variant="outlined" disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>
            {uncertain ? "Confirmar tentativa" : "Salvar débito"}
          </Button>
        </Stack>
      </Box>
    </Stack>
    </>
  );
}
