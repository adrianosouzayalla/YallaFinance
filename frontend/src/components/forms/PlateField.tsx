"use client";

import { TextField, type TextFieldProps } from "@mui/material";
import { formatPlate, isValidPlate, normalizePlate } from "@/utils/plate";

export type PlateFieldProps = Omit<
  TextFieldProps,
  "value" | "defaultValue" | "onChange" | "type" | "select" | "multiline"
> & {
  value: string | null;
  onChange(value: string): void;
};

/** Both Brazilian plate formats share a normalized seven-character value. */
export function PlateField({
  value,
  onChange,
  label = "Placa",
  error,
  helperText,
  ...props
}: PlateFieldProps) {
  const invalid = Boolean(value && value.length === 7 && !isValidPlate(value));
  return (
    <TextField
      {...props}
      label={label}
      type="text"
      value={formatPlate(value ?? "")}
      error={error || invalid}
      helperText={
        helperText ?? (invalid ? "Informe uma placa no formato ABC-1234 ou ABC1D23." : undefined)
      }
      onChange={(event) => {
        const plate = normalizePlate(event.target.value);
        if (plate.length <= 7) onChange(plate);
      }}
    />
  );
}
