"use client";
import { useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Controller, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Alert,
  Box,
  Button,
  Divider,
  FormControlLabel,
  Checkbox,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import { DateField } from "@/components/forms/DateField";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import {
  paymentRequestSchema,
  type PaymentRequestFormData,
} from "../schemas/paymentRequest.schema";
import { buildPaymentBatchInput, paymentStorageKey } from "../paymentAttempt";
import { paymentDateOnly, paymentExpirationError, paymentMaximumDate } from "../paymentDate";
import { useGeneratePaymentRequests } from "../hooks/useGeneratePaymentRequests";
import type { PaymentBatchInput, PaymentPreparation } from "@/services/api/payments/payment.types";
import { PaymentCart } from "./PaymentCart";
import { LoadingState } from "@/components/ui/LoadingState";

function storedInput(key: string): PaymentBatchInput | undefined {
  try {
    const raw = sessionStorage.getItem(key);
    return raw ? (JSON.parse(raw) as PaymentBatchInput) : undefined;
  } catch {
    return undefined;
  }
}

export function PaymentRequestForm({
  preparation,
  onRemove,
}: {
  preparation: PaymentPreparation;
  onRemove(id: string): void;
}) {
  const { contextId } = useSessionContext();
  const router = useRouter();
  const ids = preparation.charges.map((row) => row.id);
  const dueDates = preparation.charges.map((row) => row.dueDate);
  const storageKey = paymentStorageKey(contextId, ids);
  const [previous, setPrevious] = useState<PaymentBatchInput | undefined>(() =>
    storedInput(storageKey),
  );
  const busy = useRef(false);
  const mutation = useGeneratePaymentRequests();
  const [error, setError] = useState("");
  const defaultPaymentMethodIds =
    previous?.paymentMethodIds ??
    preparation.methods.reduce<string[]>((selectedIds, method) => {
      if (
        method.instrumentType &&
        preparation.methods.some(
          (candidate) =>
            selectedIds.includes(candidate.id) &&
            candidate.instrumentType === method.instrumentType,
        )
      )
        return selectedIds;
      selectedIds.push(method.id);
      return selectedIds;
    }, []);
  const form = useForm<PaymentRequestFormData>({
    resolver: zodResolver(paymentRequestSchema),
    defaultValues: {
      paymentMethodIds: defaultPaymentMethodIds,
      expiresDate: paymentDateOnly(previous?.expiresAt ?? new Date().toISOString()),
      note: previous?.note ?? "",
    },
  });
  const selectedMethods = useWatch({ control: form.control, name: "paymentMethodIds" });
  const selectedInstrumentTypes = new Set(
    preparation.methods
      .filter((method) => selectedMethods.includes(method.id) && method.instrumentType)
      .map((method) => method.instrumentType),
  );
  const expiresDate = useWatch({ control: form.control, name: "expiresDate" });
  const expirationError =
    previous && paymentDateOnly(previous.expiresAt) === expiresDate
      ? null
      : paymentExpirationError(expiresDate, preparation.maximumLinkDays, new Date(), dueDates);
  const hasSelectedMethod = selectedMethods.length > 0;
  const canReview = preparation.charges.every(
    (row) => row.eligible || (previous && row.blockReason === "ActiveRequestExists"),
  );
  async function submit(value: PaymentRequestFormData) {
    if (busy.current) return;
    busy.current = true;
    setError("");
    try {
      const input = buildPaymentBatchInput(ids, value, previous);
      const dateError =
        input === previous
          ? null
          : paymentExpirationError(
              value.expiresDate,
              preparation.maximumLinkDays,
              new Date(),
              dueDates,
            );
      if (dateError) {
        form.setError("expiresDate", { type: "validate", message: dateError });
        return;
      }
      setPrevious(input);
      // Persist only the command and keys, never protected tokens or recovered links.
      try {
        sessionStorage.setItem(storageKey, JSON.stringify(input));
      } catch {
        /* Keys remain stable during this mounted attempt. */
      }
      const result = await mutation.mutateAsync(input);
      const generated = result.items.flatMap((item) => (item.request ? [item.request.id] : []));
      const failures = result.items.filter((item) => !item.request);
      // A confirmed response resolves the uncertain command, including partial failures.
      setPrevious(undefined);
      try {
        sessionStorage.removeItem(storageKey);
      } catch {}
      if (!generated.length) {
        setError(
          failures
            .map((item) => item.message)
            .filter(Boolean)
            .join(" · ") || "Nenhuma solicitação foi gerada.",
        );
        setPrevious(undefined);
        try {
          sessionStorage.removeItem(storageKey);
        } catch {}
        return;
      }
      if (failures.length) {
        try {
          sessionStorage.setItem(
            `payment-failures:${contextId}:${generated.join(",")}`,
            JSON.stringify(
              failures.map((item) => ({ chargeId: item.chargeId, message: item.message })),
            ),
          );
        } catch {}
      }
      router.push(`/payments/result?requestIds=${generated.join(",")}`);
    } catch (failure) {
      setError(
        failure instanceof Error
          ? failure.message === "Falha na API"
            ? "Resultado não confirmado. Repita a mesma operação para recuperar as solicitações."
            : failure.message
          : "Não foi possível gerar as solicitações.",
      );
    } finally {
      busy.current = false;
    }
  }
  return (
    <>
      <LoadingState mode="blocking" open={mutation.isPending} message="Gerando solicitações..." />
      <Stack
        component="form"
        noValidate
        spacing={3}
        onSubmit={(event) => {
          void form.handleSubmit(submit)(event);
        }}
      >
        <Box
          sx={{
            display: "grid",
            gap: 3,
            gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "minmax(0, 1fr) minmax(340px, 1fr)" },
          }}
        >
          <PaymentCart
            charges={preparation.charges}
            disabled={mutation.isPending}
            onRemove={onRemove}
          />
          <PrototypePanel title="Solicitação de pagamento">
            <Stack spacing={2.5} sx={{ p: 2.5 }}>
              <Typography color="text.secondary">
                Cada cobrança terá sua própria solicitação e link de pagamento.
              </Typography>
              {!canReview && (
                <Alert severity="warning">
                  Há cobranças indisponíveis. Revise a seleção antes de gerar.
                </Alert>
              )}
              {!preparation.methods.length && (
                <Alert severity="warning">
                  Nenhuma forma de pagamento ativa cadastrada para esta empresa.
                </Alert>
              )}
              {!!preparation.methods.length && !hasSelectedMethod && (
                <Alert severity="warning">
                  Selecione pelo menos uma forma de pagamento para continuar.
                </Alert>
              )}
              <Controller
                control={form.control}
                name="paymentMethodIds"
                render={({ field, fieldState }) => (
                  <Stack>
                    <Typography sx={{ fontWeight: 600 }}>
                      Formas de pagamento autorizadas
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Selecione no máximo uma forma de pagamento por modalidade.
                    </Typography>
                    {preparation.methods.map((method) => (
                      <FormControlLabel
                        key={method.id}
                        label={method.internalDescription?.trim() || method.name}
                        control={
                          <Checkbox
                            checked={field.value.includes(method.id)}
                            disabled={
                              mutation.isPending ||
                              (!!method.instrumentType &&
                                selectedInstrumentTypes.has(method.instrumentType) &&
                                !field.value.includes(method.id))
                            }
                            onChange={(_, checked) =>
                              field.onChange(
                                checked
                                  ? [...field.value, method.id]
                                  : field.value.filter((id) => id !== method.id),
                              )
                            }
                          />
                        }
                      />
                    ))}
                    {fieldState.error && (
                      <Typography color="error" variant="body2">
                        {fieldState.error.message}
                      </Typography>
                    )}
                  </Stack>
                )}
              />
              <Controller
                control={form.control}
                name="expiresDate"
                render={({ field, fieldState }) => (
                  <DateField
                    label="Validade do link"
                    value={field.value}
                    onChange={(value) => field.onChange(value ?? "")}
                    required
                    disabled={mutation.isPending}
                    min={paymentDateOnly(new Date().toISOString())}
                    max={paymentMaximumDate(preparation.maximumLinkDays, new Date(), dueDates)}
                    error={!!fieldState.error || !!expirationError}
                    helperText={
                      fieldState.error?.message ??
                      expirationError ??
                      `Até ${preparation.maximumLinkDays} dias, sem ultrapassar o vencimento mais próximo das cobranças; válido até o fim do dia informado.`
                    }
                  />
                )}
              />
              <TextField
                label="Nota interna"
                multiline
                minRows={3}
                {...form.register("note")}
                disabled={mutation.isPending}
                error={!!form.formState.errors.note}
                helperText={form.formState.errors.note?.message ?? "Não será exibida ao pagador."}
              />
              {error && <Alert severity="error">{error}</Alert>}
            </Stack>
          </PrototypePanel>
        </Box>
        <Divider />
        <Stack
          direction="row"
          spacing={2}
          sx={{ justifyContent: "flex-end", flexWrap: "wrap", rowGap: 2 }}
        >
          <Button
            component={Link}
            href="/payments"
            variant="outlined"
            startIcon={<ArrowBackOutlinedIcon />}
            disabled={mutation.isPending}
          >
            Voltar para consulta de pagamentos
          </Button>
          <Button
            type="submit"
            variant="contained"
            startIcon={<AccountBalanceWalletOutlinedIcon />}
            disabled={
              mutation.isPending ||
              !hasSelectedMethod ||
              !!expirationError ||
              (!previous && !preparation.methods.length)
            }
          >
            {mutation.isPending ? "Gerando solicitações..." : "Gerar requisições de pagamento"}
          </Button>
        </Stack>
      </Stack>
    </>
  );
}
