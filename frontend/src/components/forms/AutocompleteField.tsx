"use client";

import { Autocomplete, CircularProgress, TextField } from "@mui/material";
import type { ReactNode } from "react";

interface AutocompleteFieldProps<T> {
  label: string;
  options: readonly T[];
  value: T | null;
  inputValue: string;
  getOptionLabel(option: T): string;
  getOptionKey(option: T): string;
  onChange(value: T | null): void;
  onInputChange(value: string): void;
  loading?: boolean;
  disabled?: boolean;
  required?: boolean;
  error?: boolean;
  helperText?: ReactNode;
  placeholder?: string;
  noOptionsText?: string;
}

/** Controlled remote suggestions. Queries and business rules belong to the caller. */
export function AutocompleteField<T>({
  label,
  options,
  value,
  inputValue,
  getOptionLabel,
  getOptionKey,
  onChange,
  onInputChange,
  loading = false,
  disabled,
  required,
  error,
  helperText,
  placeholder,
  noOptionsText = "Nenhum resultado encontrado",
}: AutocompleteFieldProps<T>) {
  return (
    <Autocomplete<T, false, false, false>
      fullWidth
      size="small"
      options={options}
      value={value}
      inputValue={inputValue}
      getOptionLabel={getOptionLabel}
      getOptionKey={getOptionKey}
      isOptionEqualToValue={(option, selected) => getOptionKey(option) === getOptionKey(selected)}
      filterOptions={(items) => items}
      onChange={(_, next) => onChange(next)}
      onInputChange={(_, text, reason) => {
        if (reason === "input" || reason === "clear") onInputChange(text);
      }}
      loading={loading}
      disabled={disabled}
      loadingText="Buscando..."
      clearText="Limpar"
      openText="Abrir sugestões"
      closeText="Fechar sugestões"
      noOptionsText={noOptionsText}
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          placeholder={placeholder}
          required={required}
          error={error}
          helperText={helperText}
          slotProps={{
            ...params.slotProps,
            input: {
              ...params.slotProps.input,
              endAdornment: (
                <>
                  {loading ? <CircularProgress size={18} aria-label="Buscando opções" /> : null}
                  {params.slotProps.input.endAdornment}
                </>
              ),
            },
          }}
        />
      )}
    />
  );
}
