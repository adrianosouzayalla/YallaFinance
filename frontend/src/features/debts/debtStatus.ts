export const debtStatuses = ["Open", "InCollection", "Paid", "Canceled"] as const;
export type DebtStatus = (typeof debtStatuses)[number];

export const debtStatusLabels: Record<DebtStatus, string> = {
  Open: "Em aberto",
  InCollection: "Em cobrança",
  Paid: "Quitado",
  Canceled: "Cancelado",
};

export function getDebtStatusLabel(status: DebtStatus): string {
  return debtStatusLabels[status];
}

export function debtStatusFromQuery(value: string | null): DebtStatus | undefined {
  if (value === null) return undefined;
  const status = debtStatuses.find((candidate) => candidate === value);
  if (status === undefined) throw new Error("INVALID_DEBT_STATUS");
  return status;
}
