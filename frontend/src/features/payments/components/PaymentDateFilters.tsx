"use client";
import { Button, FormControl, InputLabel, Select, Stack, TextField } from "@mui/material";
import { DateField } from "@/components/forms/DateField";
import type {
  PaymentChargeFilters,
  PaymentChargeStatusFilter,
} from "@/services/api/payments/payment.types";

export function PaymentDateFilters({
  value,
  disabled,
  onChange,
  onSubmit,
  onClear,
}: {
  value: PaymentChargeFilters;
  disabled: boolean;
  onChange(value: PaymentChargeFilters): void;
  onSubmit(): void;
  onClear(): void;
}) {
  return (
    <Stack
      component="form"
      direction="row"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit();
      }}
      sx={{ alignItems: "flex-end", columnGap: 1.25, flexWrap: "wrap", rowGap: 2 }}
    >
      <TextField
        id="payment-reference"
        label="Número da cobrança"
        placeholder="10-123"
        size="small"
        disabled={disabled}
        value={value.reference ?? ""}
        sx={{ width: { xs: "100%", sm: 200 } }}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { maxLength: 39 } }}
        onChange={(event) => onChange({ ...value, reference: event.target.value, page: 1 })}
      />
      <FormControl disabled={disabled} size="small" sx={{ minWidth: 180 }}>
        <InputLabel htmlFor="payment-status" shrink>
          Situação
        </InputLabel>
        <Select
          id="payment-status"
          label="Situação"
          native
          notched
          value={value.paymentStatus ?? ""}
          onChange={(event) =>
            onChange({
              ...value,
              page: 1,
              paymentStatus: (event.target.value || undefined) as
                PaymentChargeStatusFilter | undefined,
            })
          }
        >
          <option value="">Todas</option>
          <option value="awaitingPayment">Aguardando pagamento</option>
          <option value="expired">Pagamento expirado</option>
          <option value="available">Disponível</option>
        </Select>
      </FormControl>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
        <DateField
          id="payment-start"
          label="Data inicial"
          size="small"
          disabled={disabled}
          value={value.startDate}
          max={value.endDate || undefined}
          onChange={(date) => onChange({ ...value, startDate: date ?? "", page: 1 })}
        />
        <DateField
          id="payment-end"
          label="Data final"
          size="small"
          disabled={disabled}
          value={value.endDate}
          min={value.startDate || undefined}
          onChange={(date) => onChange({ ...value, endDate: date ?? "", page: 1 })}
        />
      </Stack>
      <Button type="submit" variant="contained" disabled={disabled}>
        Filtrar
      </Button>
      <Button variant="outlined" disabled={disabled} onClick={onClear}>
        Limpar
      </Button>
    </Stack>
  );
}
