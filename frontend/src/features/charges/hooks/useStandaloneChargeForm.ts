"use client";
import { useCallback } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { standaloneChargeSchema } from "../schemas/charge.schema";
import type { FieldErrors, StandaloneChargeDraft } from "../types/charge.types";

export function useStandaloneChargeForm(defaultValues: StandaloneChargeDraft) {
  const form = useForm<StandaloneChargeDraft>({
    defaultValues,
    resolver: zodResolver(standaloneChargeSchema),
  });
  const draft = useWatch({ control: form.control }) as StandaloneChargeDraft;
  const { getValues, setValue, clearErrors, setError } = form;
  const setDraft = useCallback(
    (
      input: StandaloneChargeDraft | ((current: StandaloneChargeDraft) => StandaloneChargeDraft),
    ) => {
      const next = typeof input === "function" ? input(getValues()) : input;
      for (const field of Object.keys(next) as (keyof StandaloneChargeDraft)[])
        setValue(field, next[field]);
    },
    [getValues, setValue],
  );
  const setErrors = useCallback(
    (errors: FieldErrors) => {
      clearErrors();
      for (const [field, message] of Object.entries(errors))
        setError(field as keyof StandaloneChargeDraft, { type: "manual", message });
    },
    [clearErrors, setError],
  );
  const errors: FieldErrors = {};
  for (const field of Object.keys(form.formState.errors) as (keyof StandaloneChargeDraft)[])
    errors[field] = form.formState.errors[field]?.message;
  return { ...form, draft, setDraft, errors, setErrors };
}
