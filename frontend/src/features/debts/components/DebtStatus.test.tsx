import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { DebtFilters } from "@/features/debts/components/DebtFilters";
import { StatusBadge } from "@/features/debts/components/StatusBadge";
import { getDebtStatusLabel, type DebtStatus } from "@/features/debts/debtStatus";

afterEach(cleanup);
it.each<[DebtStatus, string]>([["Open", "Em aberto"], ["InCollection", "Em cobrança"], ["Paid", "Quitado"], ["Canceled", "Cancelado"]])(
  "shows Portuguese label for %s while submitting its technical value", (status, label) => {
    const onChange = vi.fn();
    render(<><StatusBadge status={status} /><DebtFilters categories={[]} disabled={false}
      value={{ categories: [], status: "Todos", startDate: "", endDate: "", page: 1, pageSize: 5, sortDirection: "desc" }}
      onChange={onChange} onClear={vi.fn()} onSubmit={vi.fn()} /></>);
    expect(getDebtStatusLabel(status)).toBe(label);
    expect(screen.getByText(label, { selector: "span" })).toBeVisible();
    const option = screen.getByRole("option", { name: label });
    expect(option).toHaveValue(status);
    fireEvent.change(option.parentElement!, { target: { value: status } });
    expect(onChange).toHaveBeenCalledWith(expect.objectContaining({ status }));
  });
