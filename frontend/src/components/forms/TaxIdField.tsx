"use client";

import { TextField, type TextFieldProps } from "@mui/material";
import { NumberFormatBase } from "react-number-format";

export type TaxIdFieldProps = Omit<
  TextFieldProps,
  "value" | "defaultValue" | "onChange" | "type" | "select" | "multiline"
> & {
  value: string | null;
  onChange(value: string): void;
  documentType?: "auto" | "cpf" | "cnpj";
};

function formatTaxId(digits: string, documentType: NonNullable<TaxIdFieldProps["documentType"]>) {
  const cnpj = documentType === "cnpj" || (documentType === "auto" && digits.length > 11);
  const widths = cnpj ? [2, 3, 3, 4, 2] : [3, 3, 3, 2];
  const separators = cnpj ? [".", ".", "/", "-"] : [".", ".", "-"];
  let position = 0;
  return widths
    .map((width, index) => {
      // Retain excess digits here so isAllowed can reject them instead of silently truncating.
      const chunk =
        index === widths.length - 1
          ? digits.slice(position)
          : digits.slice(position, position + width);
      position += width;
      return chunk ? `${index ? separators[index - 1] : ""}${chunk}` : "";
    })
    .join("");
}

/** CPF/CNPJ presentation with a digits-only value; lookup and business validation belong to the caller. */
export function TaxIdField({
  value,
  onChange,
  documentType = "auto",
  label,
  slotProps,
  ...props
}: TaxIdFieldProps) {
  const fieldSlots: TextFieldProps["slotProps"] = {
    ...slotProps,
    htmlInput: (state) => ({
      inputMode: "numeric",
      ...(typeof slotProps?.htmlInput === "function"
        ? slotProps.htmlInput(state)
        : slotProps?.htmlInput),
    }),
  };
  return (
    <NumberFormatBase
      {...props}
      customInput={TextField}
      slotProps={fieldSlots}
      label={label ?? (documentType === "auto" ? "CPF/CNPJ" : documentType.toUpperCase())}
      type="text"
      inputMode="numeric"
      value={(value ?? "").replace(/\D/g, "")}
      valueIsNumericString
      format={(digits) => formatTaxId(digits, documentType)}
      removeFormatting={(formatted) => formatted.replace(/\D/g, "")}
      isValidInputCharacter={(character) => /\d/.test(character)}
      isAllowed={(values) => values.value.length <= (documentType === "cpf" ? 11 : 14)}
      onValueChange={(values, source) => {
        if (source.source === "event") onChange(values.value);
      }}
    />
  );
}
