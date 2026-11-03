import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { StatusBadge as DebtStatusBadge } from "@/features/debts/components/StatusBadge";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
import { JobStatusChip } from "@/features/jobs/components/JobStatusChip";

afterEach(cleanup);

it("uses the same badge appearance for settled debts, paid charges and successful jobs", () => {
  const { container } = render(<>
    <DebtStatusBadge status="Paid" />
    <PaymentStatusBadge charge={{ eligible: false, blockReason: null, settled: true }} />
    <JobStatusChip status="Success" />
  </>);
  expect(screen.getByText("Quitado")).toBeVisible();
  expect(screen.getByText("Pago")).toBeVisible();
  expect(screen.getByText("Sucesso")).toBeVisible();
  const appearances = Array.from(container.children).map((badge) => {
    const style = getComputedStyle(badge);
    return [style.backgroundColor, style.color, style.borderRadius, style.fontSize,
      style.fontWeight, style.lineHeight, style.padding, style.display];
  });
  expect(appearances[1]).toEqual(appearances[0]);
  expect(appearances[2]).toEqual(appearances[0]);
});

it("keeps available, awaiting, expired and paid payments visually distinct", () => {
  const { container } = render(<>
    <PaymentStatusBadge charge={{ eligible: true, blockReason: null }} />
    <PaymentStatusBadge charge={{ eligible: false, blockReason: "ActiveRequestExists" }} />
    <PaymentStatusBadge charge={{ eligible: true, blockReason: null,
      activity: { latestRequest: { status: "Expired", expiresAt: "2000-01-01T00:00:00Z" } } }} />
    <PaymentStatusBadge charge={{ eligible: false, blockReason: null, settled: true }} />
  </>);
  for (const label of ["Disponível", "Aguardando pagamento", "Link vencido", "Pago"])
    expect(screen.getByText(label)).toBeVisible();
  const colors = Array.from(container.children).map((badge) => getComputedStyle(badge).backgroundColor);
  expect(new Set(colors).size).toBe(4);
});

it("keeps canceled debts and pending jobs neutral with their existing labels", () => {
  const { container } = render(<><DebtStatusBadge status="Canceled" /><JobStatusChip status="Pending" /></>);
  expect(screen.getByText("Cancelado")).toBeVisible();
  expect(screen.getByText("Aguardando")).toBeVisible();
  const [debt, job] = Array.from(container.children).map((badge) => getComputedStyle(badge));
  expect(job.backgroundColor).toBe(debt.backgroundColor);
  expect(job.color).toBe(debt.color);
});
