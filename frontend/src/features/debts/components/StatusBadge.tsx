import { StatusBadge as SharedStatusBadge, type StatusBadgeTone } from "@/components/ui/StatusBadge";
import type { DebtStatus } from "@/features/debts/debtStatus";
import { getDebtStatusLabel } from "@/features/debts/debtStatus";

interface StatusBadgeProps {
  status: DebtStatus;
}

const statusTones: Record<DebtStatus, StatusBadgeTone> = {
  Open: "error",
  InCollection: "warning",
  Paid: "success",
  Canceled: "default",
};

export function StatusBadge({ status }: StatusBadgeProps) {
  return <SharedStatusBadge label={getDebtStatusLabel(status)} tone={statusTones[status]} />;
}
