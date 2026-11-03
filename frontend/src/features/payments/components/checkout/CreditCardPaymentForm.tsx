"use client";

import { useState, type FormEvent } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { Box, Button, Checkbox, FormControlLabel, Stack, TextField, Typography } from "@mui/material";
import { NumberFormatBase } from "react-number-format";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import type { PublicCardPaymentInput } from "../../types/payment.types";
import { CreditCardPreview, type CardPreviewField } from "./CreditCardPreview";

type FormValues = {
  holderName: string; number: string; expiry: string; securityCode: string; sameHolderAsCustomer: boolean;
  name: string; email: string; document: string; postalCode: string; addressNumber: string; phone: string;
};
const defaults: FormValues = { holderName: "", number: "", expiry: "", securityCode: "", sameHolderAsCustomer: true,
  name: "", email: "", document: "", postalCode: "", addressNumber: "", phone: "" };
const fields = [
  ["name", "Nome completo do titular", "name"], ["document", "CPF/CNPJ do titular", "off"],
  ["email", "E-mail do titular", "email"], ["phone", "Telefone com DDD", "tel-national"],
  ["postalCode", "CEP do titular", "postal-code"], ["addressNumber", "Número do endereço", "off"],
] as const;
const digits = (value: string) => value.replace(/\D/g, "");

export function CreditCardPaymentForm({ missingCustomerFields, disabled, onSubmit }: {
  missingCustomerFields: string[]; disabled: boolean;
  onSubmit: (input: PublicCardPaymentInput) => Promise<void>;
}) {
  const { register, control, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({ defaultValues: defaults, shouldUnregister: true });
  const sameHolder = useWatch({ control, name: "sameHolderAsCustomer" });
  const holderName = useWatch({ control, name: "holderName" });
  const number = useWatch({ control, name: "number" });
  const expiry = useWatch({ control, name: "expiry" });
  const securityCode = useWatch({ control, name: "securityCode" });
  const [focusedCardField, setFocusedCardField] = useState<CardPreviewField>(null);
  const [openedAt] = useState(() => Date.now());
  const visibleFields = fields.filter(([name]) => sameHolder ? missingCustomerFields.includes(name) : name !== "name");
  const holderNameRegistration = register("holderName", { required: "Informe o nome do titular.", maxLength: 100 });
  const securityCodeRegistration = register("securityCode", {
    pattern: { value: /^\d{3,4}$/, message: "Informe o CVV." },
    required: "Informe o CVV.",
  });

  async function submit(values: FormValues) {
    const month = values.expiry.slice(0, 2);
    const year = values.expiry.slice(2);
    const holder = Object.fromEntries(visibleFields.map(([name]) => [name, values[name].trim()]));
    const input: PublicCardPaymentInput = { holderName: values.holderName.trim(), number: digits(values.number),
      expiryMonth: month, expiryYear: year,
      securityCode: values.securityCode, sameHolderAsCustomer: values.sameHolderAsCustomer,
      submissionId: createSecureUuidV4(), holder: { ...holder, ...(!sameHolder ? { name: values.holderName.trim() } : {}) } };
    // Clear sensitive React/form state before awaiting the network result.
    reset(defaults);
    setFocusedCardField(null);
    await onSubmit(input);
  }

  return (
    <Box component="form" autoComplete="off" onSubmit={handleSubmit(submit)} sx={{ mt: 3 }}>
      <Stack spacing={3}>
        <Typography component="h3" sx={{ fontWeight: 700 }}>Dados do cartão</Typography>
        <Box sx={{ pb: 1 }}>
          <CreditCardPreview
            number={number}
            name={holderName}
            expiry={expiry}
            cvvLength={securityCode.length}
            focusedField={focusedCardField}
          />
        </Box>
        <TextField label="Nome no cartão" autoComplete="cc-name" disabled={disabled}
          {...holderNameRegistration}
          onFocus={() => setFocusedCardField("name")}
          onBlur={(event) => { holderNameRegistration.onBlur(event); setFocusedCardField(null); }}
          error={Boolean(errors.holderName)} helperText={errors.holderName?.message} />
        <Controller name="number" control={control}
          rules={{ validate: value => /^\d{13,19}$/.test(value) || "Informe um cartão válido." }}
          render={({ field }) => (
            <NumberFormatBase customInput={TextField} label="Número do cartão" autoComplete="cc-number"
              disabled={disabled} name={field.name} value={field.value} valueIsNumericString
              type="text" inputMode="numeric" onFocus={() => setFocusedCardField("number")}
              onBlur={() => { field.onBlur(); setFocusedCardField(null); }}
              slotProps={{ htmlInput: { inputMode: "numeric", maxLength: 23, ref: field.ref,
                onInput: (event: FormEvent<HTMLInputElement>) => {
                  const input = event.currentTarget;
                  const number = digits(input.value);
                  if (number.length > 19) {
                    input.value = field.value.replace(/(\d{4})(?=\d)/g, "$1 ");
                    return;
                  }
                  input.value = number.replace(/(\d{4})(?=\d)/g, "$1 ");
                  field.onChange(number);
                },
              } }}
              format={value => value.replace(/(\d{4})(?=\d)/g, "$1 ")}
              removeFormatting={value => digits(value)}
              isValidInputCharacter={character => /\d/.test(character)}
              isAllowed={values => values.value.length <= 19}
              onValueChange={(values, source) => { if (source.source === "event") field.onChange(values.value); }}
              error={Boolean(errors.number)} helperText={errors.number?.message} />
          )} />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
          <Controller name="expiry" control={control}
            rules={{ validate: value => {
              if (!/^\d{6}$/.test(value)) return "Informe a validade no formato MM/YYYY.";
              const month = Number(value.slice(0, 2));
              const year = Number(value.slice(2));
              if (month < 1 || month > 12) return "Informe uma validade válida.";
              return new Date(year, month, 1).getTime() > openedAt || "O cartão está vencido.";
            } }}
            render={({ field }) => (
              <NumberFormatBase customInput={TextField} fullWidth label="Validade (MM/YYYY)" autoComplete="cc-exp"
                disabled={disabled} name={field.name} value={field.value} valueIsNumericString
                type="text" inputMode="numeric" onFocus={() => setFocusedCardField("expiry")}
                onBlur={() => { field.onBlur(); setFocusedCardField(null); }}
                slotProps={{ htmlInput: { inputMode: "numeric", maxLength: 7, ref: field.ref } }}
                format={value => value.replace(/\D/g, "").slice(0, 6).replace(/^(\d{2})(?=\d)/, "$1/")}
                removeFormatting={value => digits(value)}
                isValidInputCharacter={character => /\d/.test(character)}
                isAllowed={values => values.value.length <= 6}
                onValueChange={(values, source) => { if (source.source === "event") field.onChange(values.value); }}
                error={Boolean(errors.expiry)} helperText={errors.expiry?.message} />
            )} />
          <TextField fullWidth label="CVV" type="password" autoComplete="cc-csc" disabled={disabled}
            slotProps={{ htmlInput: { inputMode: "numeric", maxLength: 4 } }}
            {...securityCodeRegistration}
            onFocus={() => setFocusedCardField("cvv")}
            onBlur={(event) => { securityCodeRegistration.onBlur(event); setFocusedCardField(null); }}
            error={Boolean(errors.securityCode)} helperText={errors.securityCode?.message} />
        </Stack>
        <Controller name="sameHolderAsCustomer" control={control} render={({ field }) => (
          <FormControlLabel control={<Checkbox disabled={disabled} checked={field.value}
            onChange={(_, checked) => field.onChange(checked)} onBlur={field.onBlur} name={field.name}
            slotProps={{ input: { ref: field.ref } }} />}
            label="O titular é o cliente da cobrança" />
        )} />
        {!sameHolder && <Typography variant="body2">Informe o nome completo do titular no campo do cartão e seus dados abaixo.</Typography>}
        {visibleFields.map(([name, label, autoComplete]) => (
          <TextField key={name} label={label} autoComplete={autoComplete} disabled={disabled}
            slotProps={{ htmlInput: { maxLength: name === "email" ? 254 : 100 } }}
            {...register(name, { required: "Preencha este campo.", validate: value => {
              if (name === "document") return [11, 14].includes(digits(value).length) || "Informe CPF/CNPJ válido.";
              if (name === "postalCode") return digits(value).length === 8 || "Informe um CEP válido.";
              if (name === "phone") return [10, 11, 12, 13].includes(digits(value).length) || "Informe telefone com DDD.";
              if (name === "email") return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value) || "Informe um e-mail válido.";
              return Boolean(value.trim()) || "Preencha este campo.";
            } })} error={Boolean(errors[name])} helperText={errors[name]?.message} />
        ))}
        <Button type="submit" variant="contained" disabled={disabled}>Pagar com cartão</Button>
      </Stack>
    </Box>
  );
}
