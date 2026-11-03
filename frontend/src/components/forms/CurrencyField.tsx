"use client";

import { TextField, type TextFieldProps } from "@mui/material";
import { NumberFormatBase } from "react-number-format";
import {
  formatCurrencyInput,
  decimalToCents,
  centsToDecimal,
  formatExactCurrencyInput,
} from "@/utils/currency";

export type CurrencyFieldProps = Omit<
  TextFieldProps,
  "value" | "defaultValue" | "onChange" | "type" | "select" | "multiline"
> &
  (
    | { exactDecimal?: false; value: number | null; onChange(value: number | null): void }
    | { exactDecimal: true; value: string | null; onChange(value: string | null): void }
  );

/** Brazilian currency presentation, numeric form value; business validation belongs to the caller. */
export function CurrencyField(input: CurrencyFieldProps) {
  const { value, onChange, exactDecimal, ...props } = input;
  return (
    <NumberFormatBase
      {...props}
      customInput={TextField}
      type="text"
      inputMode="numeric"
      value={
        exactDecimal
          ? decimalToCents(value ?? "")
          : value == null
            ? ""
            : String(Math.round(value * 100))
      }
      valueIsNumericString
      format={(digits) => {
        if (digits === "" || digits === "-") return digits;
        return exactDecimal
          ? formatExactCurrencyInput(digits)
          : formatCurrencyInput(Number(digits) / 100);
      }}
      removeFormatting={(formatted) => {
        const digits = formatted.replace(/\D/g, "");
        return `${formatted.startsWith("-") ? "-" : ""}${digits}`;
      }}
      isValidInputCharacter={(character) => /[\d-]/.test(character)}
      onValueChange={(values, source) => {
        if (source.source === "event") {
          if (exactDecimal) onChange(centsToDecimal(values.value));
          else onChange(values.floatValue === undefined ? null : values.floatValue / 100);
        }
      }}
    />
  );
}
