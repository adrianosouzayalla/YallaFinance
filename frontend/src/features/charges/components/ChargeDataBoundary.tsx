"use client";
import type { PropsWithChildren } from "react";
import { Alert, Button } from "@mui/material";
import { LoadingState } from "@/components/ui/LoadingState";
import { useChargeDebts } from "../hooks/useChargeDebts";
import { useSearchParams } from "next/navigation";
import { useChargeDraft } from "../context/ChargeDraftProvider";

export function ChargeDataBoundary({ children }: PropsWithChildren) {
  const params = useSearchParams();
  const { draft } = useChargeDraft();
  const source = params.get("source");
  const ids =
    source === "selected"
      ? (params.get("debtIds") ?? "").split(",").filter(Boolean)
      : source === null && draft?.source === "selected"
        ? draft.debtIds
        : [];
  const query = useChargeDebts(ids);
  if (ids.length === 0) return children;
  if (!query.data && query.isFetching)
    return <LoadingState mode="blocking" message="Carregando dados financeiros..." />;
  if (!query.data && query.isError)
    return (
      <Alert
        severity="error"
        action={<Button onClick={() => void query.refetch()}>Tentar novamente</Button>}
      >
        Não foi possível carregar os débitos. Tente novamente.
      </Alert>
    );
  if (!query.data)
    return <LoadingState mode="blocking" message="Carregando dados financeiros..." />;
  return children;
}
