"use client";
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import type { ChargeDraft } from "../types/charge.types";

interface ChargeDraftContext {
  draft: ChargeDraft | null;
  setChargeIntent: (draft: ChargeDraft) => void;
  clearChargeIntent: () => void;
}
const Context = createContext<ChargeDraftContext | null>(null);

export function ChargeDraftProvider({ children }: PropsWithChildren) {
  const [draft, setDraft] = useState<ChargeDraft | null>(null);
  const clearChargeIntent = useCallback(() => setDraft(null), []);
  const value = useMemo(
    () => ({ draft, setChargeIntent: setDraft, clearChargeIntent }),
    [draft, clearChargeIntent],
  );
  return <Context.Provider value={value}>{children}</Context.Provider>;
}

export function useChargeDraft() {
  const value = useContext(Context);
  if (!value) throw new Error("ChargeDraftProvider is required");
  return value;
}
