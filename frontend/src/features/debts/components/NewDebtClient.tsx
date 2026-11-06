"use client";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { financeTokens } from "@/theme/designTokens";
import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useCustomerSuggestions } from "@/features/customers/hooks/useCustomerSuggestions";
import { useCustomerContracts } from "@/features/customers/hooks/useCustomerContracts";
import { useFleetMovements } from "@/features/customers/hooks/useFleetMovements";
import { isValidDateOnly } from "@/utils/date";
import type { Customer } from "@/services/api/customers/customer.types";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import { ApiError } from "@/services/api/apiClient";
import { useCreateManualDebt } from "../hooks/useCreateManualDebt";
import { manualDebtSchema } from "@/services/api/debts/manualDebt.schema";
import type { ManualDebtForm, ManualDebtInput } from "@/services/api/debts/manualDebt.types";
import { readManualDebtAttempt, storeManualDebtAttempt } from "../manualDebtAttempt";
import { LoadingState } from "@/components/ui/LoadingState";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import { ManualDebtFields, customerLabel } from "./ManualDebtFields";

const defaults: ManualDebtForm = {
  customerId: "",
  contractId: "",
  categoryId: "",
  amount: "",
  occurrenceDate: "",
  dueDate: "",
  description: "",
};

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

  const blockingLoad =
    categories.isPending ||
    mutation.isPending ||
    contracts.isFetchingNextPage ||
    (customerId !== "" && (contracts.isFetching || movements.isFetching));
  return (
    <>
      <LoadingState
        mode="blocking"
        open={blockingLoad}
        message={
          mutation.isPending
            ? "Salvando débito..."
            : contracts.isFetchingNextPage
              ? "Carregando mais contratos..."
              : "Carregando dados do débito..."
        }
      />
      <Stack spacing={3}>
        <Box>
          <FinancePageTitle navigationHref="/debts/new">Débito avulso</FinancePageTitle>
          <Typography
            color="text.secondary"
            sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}
          >
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
          <ManualDebtFields
            control={control}
            register={register}
            errors={errors}
            setValue={setValue}
            locked={locked}
            customer={customer}
            search={search}
            setSearch={setSearch}
            setCustomer={setCustomer}
            suggestions={suggestions}
            onCustomerChange={changeCustomer}
            customerId={customerId}
            validOccurrence={validOccurrence}
            contracts={contracts}
            movements={movements}
            contractItems={contractItems}
            selectedContract={selectedContract}
            categories={categories}
          />
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
