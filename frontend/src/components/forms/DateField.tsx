"use client";

import { useState } from "react";
import { TextField, type TextFieldProps } from "@mui/material";
import { PatternFormat } from "react-number-format";
import { formatDate, parseDateInput } from "@/utils/date";

type DateFieldProps = Omit<
  TextFieldProps,
  "value" | "defaultValue" | "onChange" | "type" | "select" | "multiline"
> & {
  value: string | null;
  onChange(value: string | null): void;
  mode?: "native" | "masked";
  min?: string;
  max?: string;
};

/** Defaults to the existing native calendar. Both modes expose only ISO date-only values or null. */
export function DateField({ mode = "native", ...props }: DateFieldProps) {
  return mode === "masked" ? <MaskedDateField {...props} /> : <NativeDateField {...props} />;
}

function NativeDateField({ value, onChange, min, max, slotProps, ...props }: DateFieldProps) {
  return (
    <TextField
      {...props}
      type="date"
      value={value ?? ""}
      slotProps={{
        ...slotProps,
        inputLabel: (state) => ({
          shrink: true,
          ...(typeof slotProps?.inputLabel === "function"
            ? slotProps.inputLabel(state)
            : slotProps?.inputLabel),
        }),
        input: (state) => {
          const labelProps =
            typeof slotProps?.inputLabel === "function"
              ? slotProps.inputLabel(state)
              : slotProps?.inputLabel;
          const shrink = labelProps && "shrink" in labelProps ? labelProps.shrink : true;
          return {
            ...((props.variant ?? "outlined") === "outlined" ? { notched: shrink ?? true } : {}),
            ...(typeof slotProps?.input === "function" ? slotProps.input(state) : slotProps?.input),
          };
        },
        htmlInput: (state) => ({
          ...(typeof slotProps?.htmlInput === "function"
            ? slotProps.htmlInput(state)
            : slotProps?.htmlInput),
          ...(min !== undefined ? { min } : {}),
          ...(max !== undefined ? { max } : {}),
        }),
      }}
      onChange={(event) => onChange(event.target.value || null)}
    />
  );
}

function MaskedDateField({
  value,
  onChange,
  min,
  max,
  onBlur,
  error,
  helperText,
  ...props
}: DateFieldProps) {
  const [input, setInput] = useState({ value, digits: formatDate(value).replace(/\//g, "") });
  const [touched, setTouched] = useState(false);
  if (input.value !== value) {
    setInput({ value, digits: formatDate(value).replace(/\//g, "") });
    setTouched(false);
  }
  const formatted =
    input.digits.length === 8
      ? `${input.digits.slice(0, 2)}/${input.digits.slice(2, 4)}/${input.digits.slice(4)}`
      : "";
  const normalized = parseDateInput(formatted);
  const invalid =
    input.digits.length > 0 &&
    (input.digits.length === 8 || touched) &&
    (!normalized ||
      (min !== undefined && normalized < min) ||
      (max !== undefined && normalized > max));

  return (
    <PatternFormat
      {...props}
      customInput={TextField}
      type="text"
      inputMode="numeric"
      value={input.digits}
      valueIsNumericString
      format="##/##/####"
      mask="_"
      error={error || invalid}
      helperText={
        helperText ?? (invalid ? "Informe uma data válida dentro do período permitido." : undefined)
      }
      onValueChange={(values, source) => {
        if (source.source !== "event") return;
        const next = parseDateInput(values.formattedValue);
        setInput({ value: next, digits: values.value });
        onChange(next);
      }}
      onBlur={(event) => {
        setTouched(true);
        onBlur?.(event);
      }}
    />
  );
}
