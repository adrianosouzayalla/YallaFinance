"use client";

import { useCallback, useState, type SetStateAction } from "react";

/** Stores selected records independently of the visible page. The caller owns the reset scope. */
export function usePaginatedSelection<T>({ scopeKey = "" }: { scopeKey?: string } = {}) {
  const [selection, setSelection] = useState<{ scopeKey: string; rows: T[] }>({
    scopeKey,
    rows: [],
  });
  // A new context must never expose the previous context's selection.
  if (selection.scopeKey !== scopeKey) setSelection({ scopeKey, rows: [] });

  const setSelectedRows = useCallback(
    (next: SetStateAction<T[]>) => {
      setSelection((current) => {
        const rows = current.scopeKey === scopeKey ? current.rows : [];
        return { scopeKey, rows: typeof next === "function" ? next(rows) : next };
      });
    },
    [scopeKey],
  );
  const clearSelection = useCallback(() => setSelectedRows([]), [setSelectedRows]);
  return {
    selectedRows: selection.scopeKey === scopeKey ? selection.rows : [],
    setSelectedRows,
    clearSelection,
  };
}
